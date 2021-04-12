' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatchFinally_Basic()
            Dim source = <![CDATA[
Imports System

Class C
    Private Sub M(i As Integer)
        Try'BIND:"Try"
            i = 0
        Catch ex As Exception When i > 0
            Throw ex
        Finally
            i = 1
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch ex As ... Throw ex')
        Locals: Local_1: ex As System.Exception
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: ex As System.Exception) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ex')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i > 0')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        Handler: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch ex As ... Throw ex')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'Throw ex')
              ILocalReferenceOperation: ex (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'ex')
  Finally: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Finally ... i = 1')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatchFinally_Parent()
            Dim source = <![CDATA[
Imports System

Class C
    Private Sub M(i As Integer)'BIND:"Private Sub M(i As Integer)"
        Try
            i = 0
        Catch ex As Exception When i > 0
            Throw ex
        Finally
            i = 1
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: 'Private Sub ... End Sub')
  ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try ... End Try')
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try ... End Try')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Catch clauses(1):
        ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch ex As ... Throw ex')
          Locals: Local_1: ex As System.Exception
          ExceptionDeclarationOrExpression: 
            IVariableDeclaratorOperation (Symbol: ex As System.Exception) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ex')
              Initializer: 
                null
          Filter: 
            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i > 0')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Handler: 
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch ex As ... Throw ex')
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'Throw ex')
                ILocalReferenceOperation: ex (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'ex')
    Finally: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Finally ... i = 1')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_SingleCatchClause()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e As System.IO.IOException
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... IOException')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_SingleCatchClauseAndFilter()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e As System.IO.IOException When e.Message IsNot Nothing
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IPropertyReferenceOperation: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver: 
                      ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... Not Nothing')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_MultipleCatchClausesWithDifferentCaughtTypes()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e As System.IO.IOException
        Catch e As System.Exception When e.Message IsNot Nothing
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(2):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... IOException')
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.Exception
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.Exception) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IPropertyReferenceOperation: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver: 
                      ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... Not Nothing')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_MultipleCatchClausesWithDuplicateCaughtTypes()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e As System.IO.IOException
        Catch e As System.IO.IOException When e.Message IsNot Nothing
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(2):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... IOException')
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
            Initializer: 
              null
        Filter: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IPropertyReferenceOperation: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver: 
                      ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... Not Nothing')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42031: 'Catch' block never reached; 'IOException' handled above in the same Try statement.
        Catch e As System.IO.IOException When e.Message IsNot Nothing
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithTypeExpression()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch System.Exception
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: ?) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'Catch System')
        ExceptionDeclarationOrExpression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'System')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'System')
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Catch System')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31082: 'System' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
        Catch System.Exception
              ~~~~~~
BC30205: End of statement expected.
        Catch System.Exception
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithLocalReferenceExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Dim e As IO.IOException = Nothing
        Try'BIND:"Try"
        Catch e
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithParameterReferenceExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(e As IO.IOException)
        Try'BIND:"Try"
        Catch e
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.IO.IOException) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithFieldReferenceExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private e As IO.IOException = Nothing

    Private Sub M()
        Try 'BIND:"Try"'BIND:"Try 'BIND:"Try""
        Catch e
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'Try 'BIND:" ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Try 'BIND:" ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IFieldReferenceOperation: C.e As System.IO.IOException (OperationKind.FieldReference, Type: System.IO.IOException, IsInvalid) (Syntax: 'e')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Catch e')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31082: 'e' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
        Catch e
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithErrorExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: ?) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'e')
            Children(0)
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Catch e')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'e' is not declared. It may be inaccessible due to its protection level.
        Catch e
              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithInvalidExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch M2(e)
        End Try
    End Sub

    Private Shared Function M2(e As Exception) As Exception
        Return e
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: ?) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'Catch M2')
        ExceptionDeclarationOrExpression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M2')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'M2')
                  Children(1):
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Catch M2')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31082: 'M2' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
        Catch M2(e)
              ~~
BC30205: End of statement expected.
        Catch M2(e)
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchClauseWithoutCaughtTypeOrExceptionLocal()
            Dim source = <![CDATA[

Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_FinallyWithoutCatchClause()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try'BIND:"Try"
        Finally
            Console.WriteLine(s)
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(0)
  Finally: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Finally ... riteLine(s)')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_TryBlockWithLocalDeclaration()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try'BIND:"Try"
            Dim i As Integer = 0
        Finally
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
      Locals: Local_1: i As System.Int32
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Integer = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Integer = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
                Initializer: 
                  null
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(0)
  Finally: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: 'Finally')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_CatchBlockWithLocalDeclaration()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try'BIND:"Try"
        Catch ex As Exception
            Dim i As Integer = 0
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch ex As ... Integer = 0')
        Locals: Local_1: ex As System.Exception
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: ex As System.Exception) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ex')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch ex As ... Integer = 0')
            Locals: Local_1: i As System.Int32
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Integer = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Integer = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
                      Initializer: 
                        null
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_FinallyWithLocalDeclaration()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try'BIND:"Try"
        Finally
            Dim i As Integer = 0
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(0)
  Finally: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: 'Finally ... Integer = 0')
      Locals: Local_1: i As System.Int32
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim i As Integer = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i As Integer = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
                Initializer: 
                  null
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_InvalidCaughtType()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try'BIND:"Try"
        Catch i As Integer
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITryOperation (Exit Label Id: 0) (OperationKind.Try, Type: null, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClauseOperation (Exception type: System.Int32) (OperationKind.CatchClause, Type: null, IsInvalid) (Syntax: 'Catch i As Integer')
        Locals: Local_1: i As System.Int32
        ExceptionDeclarationOrExpression: 
          IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Catch i As Integer')
  Finally: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30392: 'Catch' cannot catch type 'Integer' because it is not 'System.Exception' or a class that inherits from 'System.Exception'.
        Catch i As Integer
                   ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TryBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForCatchBlock()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException When e.Message IsNot Nothing'BIND:"Catch e As IO.IOException When e.Message IsNot Nothing"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ICatchClauseOperation (Exception type: System.IO.IOException) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch e As  ... Not Nothing')
  Locals: Local_1: e As System.IO.IOException
  ExceptionDeclarationOrExpression: 
    IVariableDeclaratorOperation (Symbol: e As System.IO.IOException) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e')
      Initializer: 
        null
  Filter: 
    IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
      Left: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IPropertyReferenceOperation: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
              Instance Receiver: 
                ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
  Handler: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch e As  ... Not Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CatchBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForFinallyBlock()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try
        Finally'BIND:"Finally"
            Console.WriteLine(s)
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Finally'BIN ... riteLine(s)')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
    Expression: 
      IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
              IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of FinallyBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForCatchExceptionIdentifier()
            Dim source = <![CDATA[
Imports System
Class C
    Private Sub M(e As Exception)
        Try
        Catch e'BIND:"e"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'e')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/22299")>
        Public Sub TryCatch_GetOperationForCatchExceptionDeclaration()
            Dim source = <![CDATA[
Imports System
Class C
    Private Sub M()
        Try
        Catch e As Exception'BIND:"e"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
  Variables: Local_1: e As System.Exception
  Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForCatchFilterClause()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException When e.Message IsNot Nothing'BIND:"When e.Message IsNot Nothing"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
]]>.Value

            ' GetOperation return Nothing for CatchFilterClauseSyntax
            Assert.Null(GetOperationTreeForTest(Of CatchFilterClauseSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForCatchFilterClauseExpression()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException When e.Message IsNot Nothing'BIND:"e.Message IsNot Nothing"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
  Left: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IPropertyReferenceOperation: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'e.Message')
          Instance Receiver: 
            ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForCatchStatement()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException When e.Message IsNot Nothing'BIND:"Catch e As IO.IOException When e.Message IsNot Nothing"
        End Try
    End Sub
End Class]]>.Value

            ' GetOperation returns Nothing for CatchStatementSyntax
            Assert.Null(GetOperationTreeForTest(Of CatchStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForTryStatement()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try'BIND:"Try"
        Catch e As IO.IOException When e.Message IsNot Nothing
        End Try
    End Sub
End Class]]>.Value

            ' GetOperation returns Nothing for TryStatementSyntax
            Assert.Null(GetOperationTreeForTest(Of TryStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForEndTryStatement()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException When e.Message IsNot Nothing
        End Try'BIND:"End Try"
    End Sub
End Class]]>.Value

            ' GetOperation returns Nothing for End Try statement
            Assert.Null(GetOperationTreeForTest(Of EndBlockStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForFinallyStatement()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try
        Finally'BIND:"Finally"
            Console.WriteLine(s)
        End Try
    End Sub
End Class]]>.Value

            ' GetOperation returns Nothing for FinallyStatementSyntax
            Assert.Null(GetOperationTreeForTest(Of FinallyStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForStatementInTryBlock()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try
            Console.WriteLine(s)'BIND:"Console.WriteLine(s)"
        Catch e As IO.IOException
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
  Expression: 
    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForStatementInCatchBlock()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M()
        Try
        Catch e As IO.IOException
            Console.WriteLine(e)'BIND:"Console.WriteLine(e)"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(e)')
  Expression: 
    IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(e)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'e')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'e')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.IO.IOException) (Syntax: 'e')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TryCatch_GetOperationForStatementInFinallyBlock()
            Dim source = <![CDATA[
Imports System
Class C
    Private Shared Sub M(s As String)
        Try
        Finally
            Console.WriteLine(s)'BIND:"Console.WriteLine(s)"
        End Try
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
  Expression: 
    IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ExceptionDispatch_45()
            Dim source = <![CDATA[
Imports System
Public Class C
    Shared Sub Main()
    End Sub

    Sub M(x As Integer) 'BIND:"Sub M"
        Try
            Throw New NullReferenceException()
    label1:
            x = 1
            End
        Catch
            x = 3
            GoTo label1
        Finally
            x = 4
        end Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B0]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: Sub System.NullReferenceException..ctor()) (OperationKind.ObjectCreation, Type: System.NullReferenceException) (Syntax: 'New NullRef ... Exception()')
                  Arguments(0)
                  Initializer: 
                    null
        Block[B2] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (ProgramTermination) Block[null]
    }
    .catch {R5} (System.Exception)
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B2]
                Leaving: {R5}
                Entering: {R4}
    }
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 4')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 4')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (StructuredExceptionHandling) Block[null]
}

Block[B5] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseExe)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub FinallyDispatch_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer) 'BIND:"Sub M"
        GoTo label2

        Try
            Try
    label1:
                x = 1
    label2:
                x = 2
            Finally
                x = 3
            end Try
        Finally
            x = 4
            goto label1
        end Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30754: 'GoTo label2' is not valid because 'label2' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
        GoTo label2
             ~~~~~~
BC30101: Branching out of a 'Finally' is not valid.
            goto label1
                 ~~~~~~
BC30754: 'GoTo label1' is not valid because 'label1' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            goto label1
                 ~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B1] - Block
            Predecessors: [B4]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B0] [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 2')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
                Finalizing: {R5} {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .finally {R5}
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 3')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 3')
                      Left: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (StructuredExceptionHandling) Block[null]
    }
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 4')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 4')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B1]
            Leaving: {R6}
            Entering: {R2} {R3} {R4}
}

Block[B5] - Exit [UnReachable]
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TryFlow_30()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer) 'BIND:"Sub M"
        Try
            x = 1
        end Try
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30030: Try must have at least one 'Catch' or a 'Finally'.
        Try
        ~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TryFlow_31()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer, y as boolean) 'BIND:"Sub M"
        Try
            If y
                Exit Try
            End If
            x = 1
        end Try
        x = 2
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30030: Try must have at least one 'Catch' or a 'Finally'.
        Try
        ~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 2')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

    Next (Regular) Block[B4]
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
