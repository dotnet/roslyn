' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: 
              IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'Catch ex As ... Throw ex')
        Locals: Local_1: ex As System.Exception
        ExceptionDeclarationOrExpression: 
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ex')
            Variables: Local_1: ex As System.Exception
            Initializer: 
              null
        Filter: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
            Left: 
              IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        Handler: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Catch ex As ... Throw ex')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Throw ex')
              Expression: 
                IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'Throw ex')
                  ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
  Finally: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally ... i = 1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
            Left: 
              IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Private Sub ... End Sub')
  ITryStatement (OperationKind.TryStatement) (Syntax: 'Try ... End Try')
    Body: 
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Try ... End Try')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
          Expression: 
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
              Left: 
                IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    Catch clauses(1):
        ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'Catch ex As ... Throw ex')
          Locals: Local_1: ex As System.Exception
          ExceptionDeclarationOrExpression: 
            IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ex')
              Variables: Local_1: ex As System.Exception
              Initializer: 
                null
          Filter: 
            IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
              Left: 
                IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Handler: 
            IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Catch ex As ... Throw ex')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Throw ex')
                Expression: 
                  IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'Throw ex')
                    ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
    Finally: 
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally ... i = 1')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1')
          Expression: 
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
              Left: 
                IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  ILabeledStatement (Label: exit) (OperationKind.LabeledStatement) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression: 
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.IO.IOException
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... IOException')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body:
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression:
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.IO.IOException
            Initializer:
              null
        Filter:
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  IPropertyReferenceExpression: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver:
                      ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
            Right:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler:
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... Not Nothing')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body:
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(2):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression:
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.IO.IOException
            Initializer:
              null
        Filter:
          null
        Handler:
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... IOException')
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.Exception
        ExceptionDeclarationOrExpression:
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.Exception
            Initializer:
              null
        Filter:
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  IPropertyReferenceExpression: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver:
                      ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'e')
            Right:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler:
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... Not Nothing')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body:
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(2):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... IOException')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression:
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.IO.IOException
            Initializer:
              null
        Filter:
          null
        Handler:
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... IOException')
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... Not Nothing')
        Locals: Local_1: e As System.IO.IOException
        ExceptionDeclarationOrExpression:
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
            Variables: Local_1: e As System.IO.IOException
            Initializer:
              null
        Filter:
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
            Left:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  IPropertyReferenceExpression: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
                    Instance Receiver:
                      ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
            Right:
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        Handler:
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... Not Nothing')
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: ?) (OperationKind.CatchClause, IsInvalid) (Syntax: 'Catch System')
        ExceptionDeclarationOrExpression: 
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'System')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'System')
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Catch System')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e')
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'Try 'BIND:" ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Try 'BIND:" ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause, IsInvalid) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IFieldReferenceExpression: C.e As System.IO.IOException (OperationKind.FieldReferenceExpression, Type: System.IO.IOException, IsInvalid) (Syntax: 'e')
            Instance Receiver: 
              IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid, IsImplicit) (Syntax: 'e')
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Catch e')
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: ?) (OperationKind.CatchClause, IsInvalid) (Syntax: 'Catch e')
        ExceptionDeclarationOrExpression: 
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'e')
            Children(0)
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Catch e')
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: ?) (OperationKind.CatchClause, IsInvalid) (Syntax: 'Catch M2')
        ExceptionDeclarationOrExpression: 
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'M2')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'M2')
                  Children(1):
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Catch M2')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'Catch')
        ExceptionDeclarationOrExpression: 
          null
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(0)
  Finally: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally ... riteLine(s)')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
                  IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
      Locals: Local_1: i As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
          Variables: Local_1: i As System.Int32
          Initializer: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Catch clauses(0)
  Finally: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Finally')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.Exception) (OperationKind.CatchClause) (Syntax: 'Catch ex As ... Integer = 0')
        Locals: Local_1: ex As System.Exception
        ExceptionDeclarationOrExpression: 
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ex')
            Variables: Local_1: ex As System.Exception
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Catch ex As ... Integer = 0')
            Locals: Local_1: i As System.Int32
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
                Variables: Local_1: i As System.Int32
                Initializer: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
ITryStatement (OperationKind.TryStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(0)
  Finally: 
    IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Finally ... Integer = 0')
      Locals: Local_1: i As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
          Variables: Local_1: i As System.Int32
          Initializer: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
ITryStatement (OperationKind.TryStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Try'BIND:"T ... End Try')
  Catch clauses(1):
      ICatchClause (Exception type: System.Int32) (OperationKind.CatchClause, IsInvalid) (Syntax: 'Catch i As Integer')
        Locals: Local_1: i As System.Int32
        ExceptionDeclarationOrExpression: 
          IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i')
            Variables: Local_1: i As System.Int32
            Initializer: 
              null
        Filter: 
          null
        Handler: 
          IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Catch i As Integer')
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
ICatchClause (Exception type: System.IO.IOException) (OperationKind.CatchClause) (Syntax: 'Catch e As  ... Not Nothing')
  Locals: Local_1: e As System.IO.IOException
  ExceptionDeclarationOrExpression:
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'e')
      Variables: Local_1: e As System.IO.IOException
      Initializer:
        null
  Filter:
    IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
      Left:
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand:
            IPropertyReferenceExpression: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
              Instance Receiver:
                ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
      Right:
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand:
            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  Handler:
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch e As  ... Not Nothing')
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
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally'BIN ... riteLine(s)')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
    Expression: 
      IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
        Instance Receiver: 
          null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
              IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
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
IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Exception) (Syntax: 'e')
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
IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e.Message IsNot Nothing')
  Left:
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e.Message')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IPropertyReferenceExpression: ReadOnly Property System.Exception.Message As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'e.Message')
          Instance Receiver:
            ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
  Right:
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
  Expression: 
    IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
            IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(e)')
  Expression:
    IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(e)')
      Instance Receiver:
        null
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'e')
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'e')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.IO.IOException) (Syntax: 'e')
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
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(s)')
  Expression: 
    IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 's')
            IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExpressionStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
