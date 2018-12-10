' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_SingleClause_SimpleArray()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1) As Integer
        ReDim intArray(2)'BIND:"ReDim intArray(2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim intArray(2)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimClause_SimpleArray()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1) As Integer
        ReDim intArray(2)'BIND:"intArray(2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
  Operand: 
    ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
  DimensionSizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RedimClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimOperand_SimpleArray()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1) As Integer
        ReDim intArray(2)'BIND:"intArray"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimSize_SimpleArray()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1) As Integer
        ReDim intArray(2)'BIND:"2"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LiteralExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_SingleClause_MultiDimensionalArray()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1, 1) As Integer
        ReDim intArray(2, 1)'BIND:"ReDim intArray(2, 1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim intArray(2, 1)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2, 1)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32(,)) (Syntax: 'intArray')
        DimensionSizes(2):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_MultipleClause_DifferentDimensionalArrays()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        Dim intArray(1) As Integer
        ReDim intArray(2), x(1, 1)'BIND:"ReDim intArray(2), x(1, 1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim intAr ... 2), x(1, 1)')
  Clauses(2):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(1, 1)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')
        DimensionSizes(2):
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
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimClause_FirstClauseFromMultipleClauses()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        Dim intArray(1) As Integer
        ReDim intArray(2), x(1, 1)'BIND:"intArray(2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
  Operand: 
    ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
  DimensionSizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
        Left: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RedimClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimClause_SecondClauseFromMultipleClauses()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        Dim intArray(1) As Integer
        ReDim intArray(2), x(1, 1)'BIND:"x(1, 1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(1, 1)')
  Operand: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')
  DimensionSizes(2):
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
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RedimClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_Preserve_SingleClause()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        Dim intArray(1) As Integer
        ReDim Preserve intArray(2)'BIND:"ReDim Preserve intArray(2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (Preserve) (OperationKind.ReDim, Type: null) (Syntax: 'ReDim Prese ... intArray(2)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_Preserve_MultipleClause()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        Dim intArray(1) As Integer
        ReDim Preserve intArray(2), x(1, 1)'BIND:"ReDim Preserve intArray(2), x(1, 1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (Preserve) (OperationKind.ReDim, Type: null) (Syntax: 'ReDim Prese ... 2), x(1, 1)')
  Clauses(2):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(2)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'intArray')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(1, 1)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')
        DimensionSizes(2):
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
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_NoOperandOrIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        ReDim'BIND:"ReDim"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: '')
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: '')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        ReDim'BIND:"ReDim"
             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_Preserve_NoOperandOrIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        ReDim Preserve'BIND:"ReDim Preserve"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (Preserve) (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim Preserve')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: '')
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: '')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        ReDim Preserve'BIND:"ReDim Preserve"
                      ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_NoIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x() As Integer)
        ReDim x'BIND:"ReDim x"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x'BIND:"ReDim x"')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'x'BIND:"ReDim x"')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(), IsInvalid) (Syntax: 'x')
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30670: 'ReDim' statements require a parenthesized list of the new bounds of each dimension of the array.
        ReDim x'BIND:"ReDim x"
              ~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_NoOperand()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M()
        ReDim (1)'BIND:"ReDim (1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim (1)'B ... "ReDim (1)"')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: '(1)'BIND:"ReDim (1)"')
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '(1)')
            Children(1):
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '(1)')
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30074: Constant cannot be the target of an assignment.
        ReDim (1)'BIND:"ReDim (1)"
              ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_NonArrayOperandWithoutIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        ReDim x'BIND:"ReDim x"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x'BIND:"ReDim x"')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'x'BIND:"ReDim x"')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30049: 'Redim' statement requires an array.
        ReDim x'BIND:"ReDim x"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_NonArrayOperandWithIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        ReDim x(1)'BIND:"ReDim x(1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x(1)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'x(1)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30049: 'Redim' statement requires an array.
        ReDim x(1)'BIND:"ReDim x(1)"
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_ChangeArrayDimensions()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        ReDim x(1)'BIND:"ReDim x(1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x(1)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'x(1)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,), IsInvalid) (Syntax: 'x')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsInvalid, IsImplicit) (Syntax: '1')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30415: 'ReDim' cannot change the number of dimensions of an array.
        ReDim x(1)'BIND:"ReDim x(1)"
              ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_MissingIndexArgument()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer)
        ReDim x(1,)'BIND:"ReDim x(1,)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x(1,)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'x(1,)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')
        DimensionSizes(2):
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
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30306: Array subscript expression missing.
        ReDim x(1,)'BIND:"ReDim x(1,)"
                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(30175, "https://github.com/dotnet/roslyn/issues/30175")>
        Public Sub ReDimStatement_ErrorCase_MissingClause()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x() As Integer)
        ReDim x(1), 'BIND:"ReDim x(1), "
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim x(1), ')
  Clauses(2):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(1)')
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
        DimensionSizes(1):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
              Left: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: '')
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: '')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
        DimensionSizes(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        ReDim x(1), 'BIND:"ReDim x(1), "
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_NoControlFlow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x() As Integer, a As Integer)'BIND:"Public Sub M(x() As Integer, a As Integer)"
        ReDim x(a)
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim x(a)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(a)')
                Operand: 
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Left: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInClauseOperand()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x1() As Integer, x2() As Integer, a As Integer)'BIND:"Public Sub M(x1() As Integer, x2() As Integer, a As Integer)"
        ReDim If(x1, x2)(a)
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32(), IsInvalid) (Syntax: 'x1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsInvalid, IsImplicit) (Syntax: 'x1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsInvalid, IsImplicit) (Syntax: 'x1')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32(), IsInvalid) (Syntax: 'x2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim If(x1, x2)(a)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'If(x1, x2)(a)')
                    Operand: 
                      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')
                        Children(1):
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a')
                          Left: 
                            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim If(x1, x2)(a)
              ~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInFirstIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)'BIND:"Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)"
        ReDim x(If(a1, a2), b)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim x(If(a1, a2), b)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(If(a1, a2), b)')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'x')
                    DimensionSizes(2):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'b')
                          Left: 
                            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b')

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
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInSecondIndex()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)'BIND:"Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)"
        ReDim x(b, If(a1, a2))
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Left: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim x(b, If(a1, a2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(b, If(a1, a2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'x')
                    DimensionSizes(2):
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

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
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInMultipleDimensionSizes()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)'BIND:"Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)"
        ReDim x(If(a1, a2), If(b1, b2))
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')

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
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                      Value: 
                        IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(a1, a2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

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
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IParameterReferenceOperation: b1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b1')

            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                Leaving: {R4}

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                      Arguments(0)

            Next (Regular) Block[B9]
                Leaving: {R4}
    }

    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b2')

        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim x(If( ... If(b1, b2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(If(a1, a2 ... If(b1, b2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'x')
                    DimensionSizes(2):
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(b1, b2)')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInOperandAndDimensionSizes()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x1(,) As Integer, x2(,) As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)'BIND:"Public Sub M(x1(,) As Integer, x2(,) As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)"
        ReDim If(x1, x2)(If(a1, a2), If(b1, b2))
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2] [5] [7]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
                      Value: 
                        IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32(,), IsInvalid) (Syntax: 'x1')

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'x1')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'x1')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x2')
                  Value: 
                    IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32(,), IsInvalid) (Syntax: 'x2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        .locals {R5}
        {
            CaptureIds: [3]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                      Value: 
                        IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                    Leaving: {R5}

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                          Arguments(0)

                Next (Regular) Block[B8]
                    Leaving: {R5}
        }

        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(a1, a2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

            Next (Regular) Block[B9]
                Leaving: {R4}
                Entering: {R6}
    }
    .locals {R6}
    {
        CaptureIds: [6]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IParameterReferenceOperation: b1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b1')

            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                Leaving: {R6}

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                      Arguments(0)

            Next (Regular) Block[B12]
                Leaving: {R6}
    }

    Block[B11] - Block
        Predecessors: [B9]
        Statements (1)
            IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b2')

        Next (Regular) Block[B12]
    Block[B12] - Block
        Predecessors: [B10] [B11]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsInvalid) (Syntax: 'ReDim If(x1 ... If(b1, b2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null, IsInvalid) (Syntax: 'If(x1, x2)( ... If(b1, b2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'If(x1, x2)')
                    DimensionSizes(2):
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(b1, b2)')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B12]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim If(x1, x2)(If(a1, a2), If(b1, b2))
              ~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInFirstClause()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b As Integer)'BIND:"Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b As Integer)"
        ReDim x1(If(a1, a2)), x2(b)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
              Value: 
                IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(If ... a2)), x2(b)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x1(If(a1, a2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'x1')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Block
    Predecessors: [B5]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(If ... a2)), x2(b)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x2(b)')
                Operand: 
                  IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x2')
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInSecondClause()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b As Integer)'BIND:"Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b As Integer)"
        ReDim x1(b), x2(If(a1, a2))
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(b) ... If(a1, a2))')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x1(b)')
                Operand: 
                  IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x1')
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Left: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b')

    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x2')

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B3]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(b) ... If(a1, a2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x2(If(a1, a2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'x2')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInMultipleClauses()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)'BIND:"Public Sub M(x1() As Integer, x2() As Integer, a1 As Integer?, a2 As Integer, b1 As Integer?, b2 As Integer)"
        ReDim x1(If(a1, a2)), x2(If(b1, b2))
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
              Value: 
                IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(If ... If(b1, b2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x1(If(a1, a2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'x1')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
            Entering: {R3}
}
.locals {R3}
{
    CaptureIds: [3] [5]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
              Value: 
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x2')

        Next (Regular) Block[B7]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [4]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IParameterReferenceOperation: b1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'b1')

            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                Leaving: {R4}

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'b1')
                      Arguments(0)

            Next (Regular) Block[B10]
                Leaving: {R4}
    }

    Block[B9] - Block
        Predecessors: [B7]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b2')

        Next (Regular) Block[B10]
    Block[B10] - Block
        Predecessors: [B8] [B9]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim x1(If ... If(b1, b2))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x2(If(b1, b2))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'x2')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b1, b2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(b1, b2)')

        Next (Regular) Block[B11]
            Leaving: {R3}
}

Block[B11] - Exit
    Predecessors: [B10]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_NoControlFlowPropertyInvocationReturningArray()
            Dim source = <![CDATA[
Module Module1
    Public Sub Main(a1 As Integer, a2 As Integer, a3 As Integer)'BIND:"Public Sub Main(a1 As Integer, a2 As Integer, a3 As Integer)"
        ReDim X(a1)(a2), Y(a3)
    End Sub

    Property X(z As Integer) As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property

    Property Y As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Module
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1)(a2), Y(a3)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'X(a1)(a2)')
                Operand: 
                  IPropertyReferenceOperation: Property Module1.X(z As System.Int32) As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'X(a1)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'a1')
                          IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a1')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a2')
                      Left: 
                        IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a2')

        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1)(a2), Y(a3)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'Y(a3)')
                Operand: 
                  IPropertyReferenceOperation: Property Module1.Y As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'Y')
                    Instance Receiver: 
                      null
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a3')
                      Left: 
                        IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a3')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInFirstClauseOperand_PropertyInvocationReturningArray()
            Dim source = <![CDATA[
Module Module1
    Public Sub Main(a1 As Integer?, a2 As Integer, a3 As Integer, a4 As Integer)'BIND:"Public Sub Main(a1 As Integer?, a2 As Integer, a3 As Integer, a4 As Integer)"
        ReDim X(If(a1, a2))(a3), Y(a4)
    End Sub

    Property X(z As Integer) As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property

    Property Y As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Module
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(If( ... (a3), Y(a4)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'X(If(a1, a2))(a3)')
                    Operand: 
                      IPropertyReferenceOperation: Property Module1.X(z As System.Int32) As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'X(If(a1, a2))')
                        Instance Receiver: 
                          null
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'If(a1, a2)')
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a3')
                          Left: 
                            IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a3')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a3')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(If( ... (a3), Y(a4)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'Y(a4)')
                Operand: 
                  IPropertyReferenceOperation: Property Module1.Y As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'Y')
                    Instance Receiver: 
                      null
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a4')
                      Left: 
                        IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a4')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a4')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInFirstClauseDimension_PropertyInvocationReturningArray()
            Dim source = <![CDATA[
Module Module1
    Public Sub Main(a1 As Integer, a2 As Integer?, a3 As Integer, a4 As Integer)'BIND:"Public Sub Main(a1 As Integer, a2 As Integer?, a3 As Integer, a4 As Integer)"
        ReDim X(a1)(If(a2, a3)), Y(a4)
    End Sub

    Property X(z As Integer) As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property

    Property Y As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Module
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'X(a1)')
              Value: 
                IPropertyReferenceOperation: Property Module1.X(z As System.Int32) As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'X(a1)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'a1')
                        IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a2')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a2')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a3')
              Value: 
                IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a3')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1) ... a3)), Y(a4)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'X(a1)(If(a2, a3))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'X(a1)')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a2, a3)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a2, a3)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a2, a3)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Block
    Predecessors: [B5]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1) ... a3)), Y(a4)')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'Y(a4)')
                Operand: 
                  IPropertyReferenceOperation: Property Module1.Y As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'Y')
                    Instance Receiver: 
                      null
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a4')
                      Left: 
                        IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a4')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a4')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlowInSecondClause_PropertyInvocationReturningArray()
            Dim source = <![CDATA[
Module Module1
    Public Sub Main(a1 As Integer, a2 As Integer, a3 As Integer?, a4 As Integer)'BIND:"Public Sub Main(a1 As Integer, a2 As Integer, a3 As Integer?, a4 As Integer)"
        ReDim X(a1)(a2), Y(If(a3, a4))
    End Sub

    Property X(z As Integer) As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property

    Property Y As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Module
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1) ... If(a3, a4))')
          Clauses(1):
              IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'X(a1)(a2)')
                Operand: 
                  IPropertyReferenceOperation: Property Module1.X(z As System.Int32) As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'X(a1)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'a1')
                          IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a1')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                DimensionSizes(1):
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'a2')
                      Left: 
                        IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'a2')

    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Y')
              Value: 
                IPropertyReferenceOperation: Property Module1.Y As System.Int32() (Static) (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'Y')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a3')
                  Value: 
                    IParameterReferenceOperation: a3 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a3')

            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a3')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a3')
                Leaving: {R2}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a3')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a3')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a3')
                      Arguments(0)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B3]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a4')
              Value: 
                IParameterReferenceOperation: a4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a4')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IReDimOperation (OperationKind.ReDim, Type: null, IsImplicit) (Syntax: 'ReDim X(a1) ... If(a3, a4))')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'Y(If(a3, a4))')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'Y')
                    DimensionSizes(1):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a3, a4)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a3, a4)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a3, a4)')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ReDimStatement_ControlFlow_PreserveFlag()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)'BIND:"Public Sub M(x(,) As Integer, a1 As Integer?, a2 As Integer, b As Integer)"
        ReDim Preserve x(If(a1, a2), b)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IReDimOperation (Preserve) (OperationKind.ReDim, Type: null) (Syntax: 'ReDim Prese ... a1, a2), b)')
              Clauses(1):
                  IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'x(If(a1, a2), b)')
                    Operand: 
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32(,), IsImplicit) (Syntax: 'x')
                    DimensionSizes(2):
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a1, a2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(a1, a2)')
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'b')
                          Left: 
                            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'b')

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
    End Class
End Namespace
