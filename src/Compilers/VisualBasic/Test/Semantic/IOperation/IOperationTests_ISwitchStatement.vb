' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_Simple()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Exit Select')
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_SwitchSectionWithMultipleCaseClauses()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0, 1
                Console.WriteLine(0)
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (2 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0, 1 ... riteLine(0)')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '1')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0, 1 ... riteLine(0)')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(0)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(0)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '0')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_MultipleSwitchSections()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
                Console.WriteLine(0)
            Case 1
                Console.WriteLine(1)
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... riteLine(0)')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... riteLine(0)')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(0)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(0)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '0')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 1 ... riteLine(1)')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '1')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 1 ... riteLine(1)')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(1)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(1)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '1')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_ConversionInSwitchGoverningExpression()
            Dim source = <![CDATA[
Option Strict On

Class Test
    Sub M(number As Object)
        Select Case DirectCast(number, Integer)'BIND:"Select Case DirectCast(number, Integer)"
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'DirectCast( ... r, Integer)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_CaseLabelWithImplicitConversionToSwitchGoverningType()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Byte)
        Select Case number'BIND:"Select Case number"
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Byte) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 0) (Syntax: '0')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/22516"), WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_CaseLabelWithExplicitConversionToSwitchGoverningType()
            Dim source = <![CDATA[
Option Strict On

Class Test
    Const x As Object = Nothing
    Sub M(number As Integer, o As Object)
        Select Case number'BIND:"Select Case number"
            Case DirectCast(o, Integer)
            Case DirectCast(x, Integer)
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case Direct ... o, Integer)')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'DirectCast(o, Integer)')
                Value: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'DirectCast(o, Integer)')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'o')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case Direct ... o, Integer)')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case Direct ... x, Integer)')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'DirectCast(x, Integer)')
                Value: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 0) (Syntax: 'DirectCast(x, Integer)')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: IFieldReferenceExpression: Test.x As System.Object (Static) (OperationKind.FieldReferenceExpression, Type: System.Object, Constant: null) (Syntax: 'x')
                        Instance Receiver: null
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case Direct ... x, Integer)')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_Nested()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer, number2 As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
                Select Case number2
                    Case 0
                End Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... End Select')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... End Select')
                ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
                  Switch expression: IParameterReferenceExpression: number2 (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number2')
                  Sections:
                      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
                          Clauses:
                              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Body:
                              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_DuplicateCaseLabelsInSwitchSection()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0, 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (2 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0, 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0, 0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_DuplicateCaseLabelsAcrossSwitchSections()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_NonConstantCaseLabel()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer, number2 As Integer)
        Select Case number'BIND:"Select Case number"
            Case number2
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case number2')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'number2')
                Value: IParameterReferenceExpression: number2 (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number2')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case number2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_EnumType()
            Dim source = <![CDATA[
Class Test
    Enum State
        Active = 0
        Inactive = 2
    End Enum

    Sub M(state As State)
        Select Case state'BIND:"Select Case state"
            Case State.Active
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: state (OperationKind.ParameterReferenceExpression, Type: Test.State) (Syntax: 'state')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case State.Active')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'State.Active')
                Value: IFieldReferenceExpression: Test.State.Active (Static) (OperationKind.FieldReferenceExpression, Type: Test.State, Constant: 0) (Syntax: 'State.Active')
                    Instance Receiver: null
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case State.Active')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_DefaultLabel()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case Else ... Exit Select')
          Clauses:
              IDefaultCaseClause (CaseKind.Default) (OperationKind.CaseClause) (Syntax: 'Case Else')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case Else ... Exit Select')
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_SwitchSectionWithLocalDeclarataion()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
                Dim number2 As Integer = 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Integer = 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Integer = 0')
                Locals: Local_1: number2 As System.Int32
                IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim number2 ... Integer = 0')
                  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'number2')
                    Variables: Local_1: number2 As System.Int32
                    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_DuplicateLocalDeclarationAcrossSwitchSections()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case 0
                Dim number2 As Integer = 0
            Case 1
                Dim number2 As Integer = 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Integer = 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Integer = 0')
                Locals: Local_1: number2 As System.Int32
                IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim number2 ... Integer = 0')
                  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'number2')
                    Variables: Local_1: number2 As System.Int32
                    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 1 ... Integer = 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '1')
                Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'Case 1 ... Integer = 0')
                Locals: Local_1: number2 As System.Int32
                IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim number2 ... Integer = 0')
                  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'number2')
                    Variables: Local_1: number2 As System.Int32
                    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_NoExpression()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case'BIND:"Select Case"
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: null
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Select Case'BIND:"Select Case"
                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_NoSections()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (0 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GoverningExpressionWithErrorType()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Select Case num'BIND:"Select Case num"
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'num')
      Children(0)
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
                Value: null
          Body:
              IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'num' is not declared. It may be inaccessible due to its protection level.
        Select Case num'BIND:"Select Case num"
                    ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForGoverningExpression()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"number"
            Case 0
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForSwitchSection()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0'BIND:"Case 0"
                Exit Select
            Case 1
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0'BIND ... Exit Select')
    Clauses:
        ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
          Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    Body:
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0'BIND ... Exit Select')
          IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CaseBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForSimpleCaseClause()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0'BIND:"0"
                Exit Select
            Case 1
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: '0')
  Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SimpleCaseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForRangeCaseClause()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0 To 10'BIND:"0 To 10"
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRangeCaseClause (CaseKind.Range) (OperationKind.CaseClause) (Syntax: '0 To 10')
  Min: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Max: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RangeCaseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForRelationalCaseClause()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case < 10'BIND:"< 10"
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IRelationalCaseClause (Relational operator kind: BinaryOperatorKind.LessThan) (CaseKind.Relational) (OperationKind.CaseClause) (Syntax: '< 10')
  Value: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of RelationalCaseClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForSelectStatement()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number'BIND:"Select Case number"
            Case < 10
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Assert.Null(GetOperationTreeForTest(Of SelectStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForCaseStatement()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0'BIND:"Case 0"
        End Select
    End Sub
End Class]]>.Value

            Assert.Null(GetOperationTreeForTest(Of CaseStatementSyntax)(source).operation)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForExitSelectStatement()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0
                Exit Select'BIND:"Exit Select"
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ExitStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")>
        Public Sub ISwitchStatement_GetOperationForEndSelectStatement()
            Dim source = <![CDATA[
Class Test
    Sub M(number As Integer)
        Select Case number
            Case 0
                Exit Select
        End Select'BIND:"End Select"
    End Sub
End Class]]>.Value

            Assert.Null(GetOperationTreeForTest(Of EndBlockStatementSyntax)(source).operation)
        End Sub
    End Class
End Namespace
