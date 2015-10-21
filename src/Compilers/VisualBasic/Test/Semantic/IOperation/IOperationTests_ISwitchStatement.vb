' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_Simple()
            Dim source = <![CDATA[
Class Program
    Shared Sub Main(args As String())
        Dim number As Integer = 1
        Select Case number'BIND:"Select Case number"
            Case 0
                System.Console.WriteLine("0")
                Exit Select
            Case 1
                System.Console.WriteLine("1")
                Exit Select
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '0')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("0")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("0")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"0"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "0") (Syntax: '"0"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 1 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '1')
                Value: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 1 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("1")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("1")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"1"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "1") (Syntax: '"1"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_Nested()
            Dim source = <![CDATA[
Class Test
    Public Shared Sub Main()
        Dim array As Integer() = {4, 10, 14}
        Select Case array(0)'BIND:"Select Case array(0)"
            Case 3
                System.Console.WriteLine(3)
                ' Not reached.
                Exit Select

            Case 4
                System.Console.WriteLine(4)
                ' ... Use nested switch.
                Select Case array(1)
                    Case 10
                        System.Console.WriteLine(10)
                        Exit Select
                End Select
                Exit Select
        End Select
    End Sub

End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array(0)')
      Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'array')
      Indices(1):
          ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 3 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '3')
                Value: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 3 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(3)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(3)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '3')
                            ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 4 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '4')
                Value: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
          Body:
              IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'Case 4 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(4)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(4)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '4')
                            ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                            InConversion: null
                            OutConversion: null
                ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
                  Switch expression: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array(1)')
                      Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'array')
                      Indices(1):
                          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  Sections:
                      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 10 ... Exit Select')
                          Clauses:
                              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '10')
                                Value: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                          Body:
                              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 10 ... Exit Select')
                                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(10)')
                                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... iteLine(10)')
                                      Instance Receiver: null
                                      Arguments(1):
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '10')
                                            ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                                            InConversion: null
                                            OutConversion: null
                                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_FallThroughError()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main()
        Dim value As Integer = 0
        ' ... Every switch statement must be terminated.
        Select Case value'BIND:"Select Case value"
            Case 0
                System.Console.WriteLine("Zero")
            Case 1
                System.Console.WriteLine("One")
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... ine("Zero")')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '0')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... ine("Zero")')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ine("Zero")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ine("Zero")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"Zero"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Zero") (Syntax: '"Zero"')
                            InConversion: null
                            OutConversion: null
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 1 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '1')
                Value: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 1 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... Line("One")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... Line("One")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"One"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "One") (Syntax: '"One"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_Duplicate()
            Dim source = <![CDATA[
Class Program
    Shared Sub Main(args As String())f
        Dim number As Integer = 1
        Select Case number'BIND:"Select Case number"
            Case 0
                System.Console.WriteLine("0")
                Exit Select
            Case 0
                System.Console.WriteLine("0")
                Exit Select
            Case 1
                System.Console.WriteLine("1")
                Exit Select
        End Select
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (3 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '0')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("0")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("0")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"0"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "0") (Syntax: '"0"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '0')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("0")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("0")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"0"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "0") (Syntax: '"0"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 1 ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '1')
                Value: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 1 ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("1")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("1")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"1"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "1") (Syntax: '"1"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_ConstantValueNotRequired()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main()
        Dim number As Integer = 0
        Dim test As Integer = 10
        Select Case number'BIND:"Select Case number"
            Case test + 1
                System.Console.WriteLine(100)
                Return
            Case 0
                System.Console.WriteLine(0)
                Return
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case test + ... Return')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'test + 1')
                Value: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'test + 1')
                    Left: ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'test')
                    Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case test + ... Return')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine(100)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(100)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '100')
                            ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
                            InConversion: null
                            OutConversion: null
                IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return')
                  ReturnedValue: null
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case 0 ... Return')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: '0')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case 0 ... Return')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(0)')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(0)')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '0')
                            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                            InConversion: null
                            OutConversion: null
                IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return')
                  ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(), WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")>
        Public Sub ISwitchStatement_EnumType()
            Dim source = <![CDATA[
Class Program
    Public Enum State
        Active = 1
        Inactive = 2
    End Enum
    Public Shared Sub Main()
        Dim state__1 As State = State.Active

        Select Case state__1'BIND:"Select Case state__1"
            Case State.Active
                System.Console.WriteLine("A")
                Exit Select
            Case State.Inactive
                System.Console.WriteLine("I")
                Exit Select
            Case Else
                Throw New System.Exception(System.[String].Format("Unknown state: {0}", state__1))
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (3 cases) (OperationKind.SwitchStatement) (Syntax: 'Select Case ... End Select')
  Switch expression: ILocalReferenceExpression: state__1 (OperationKind.LocalReferenceExpression, Type: Program.State) (Syntax: 'state__1')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case State. ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.EnumEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'State.Active')
                Value: IFieldReferenceExpression: Program.State.Active (Static) (OperationKind.FieldReferenceExpression, Type: Program.State, Constant: 1) (Syntax: 'State.Active')
                    Instance Receiver: null
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case State. ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("A")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("A")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"A"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "A") (Syntax: '"A"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case State. ... Exit Select')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.EnumEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'State.Inactive')
                Value: IFieldReferenceExpression: Program.State.Inactive (Static) (OperationKind.FieldReferenceExpression, Type: Program.State, Constant: 2) (Syntax: 'State.Inactive')
                    Instance Receiver: null
          Body:
              IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Case State. ... Exit Select')
                IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... teLine("I")')
                  Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine("I")')
                      Instance Receiver: null
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"I"')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "I") (Syntax: '"I"')
                            InConversion: null
                            OutConversion: null
                IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'Case Else ...  state__1))')
          Clauses:
              IDefaultCaseClause (CaseKind.Default) (OperationKind.DefaultCaseClause) (Syntax: 'Case Else')
          Body:
              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case Else ...  state__1))')
                IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'Throw New S ...  state__1))')
                  ThrownObject: IObjectCreationExpression (Constructor: Sub System.Exception..ctor(message As System.String)) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'New System. ...  state__1))')
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: 'System.[Str ... , state__1)')
                            IInvocationExpression (Function System.String.Format(format As System.String, arg0 As System.Object) As System.String) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'System.[Str ... , state__1)')
                              Instance Receiver: null
                              Arguments(2):
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '"Unknown state: {0}"')
                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Unknown state: {0}") (Syntax: '"Unknown state: {0}"')
                                    InConversion: null
                                    OutConversion: null
                                  IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'state__1')
                                    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'state__1')
                                      Operand: ILocalReferenceExpression: state__1 (OperationKind.LocalReferenceExpression, Type: Program.State) (Syntax: 'state__1')
                                    InConversion: null
                                    OutConversion: null
                            InConversion: null
                            OutConversion: null
                      Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        Protected Overrides Function GetCompilationForEmit(source As IEnumerable(Of String), additionalRefs As IEnumerable(Of CodeAnalysis.MetadataReference), options As CodeAnalysis.CompilationOptions, parseOptions As CodeAnalysis.ParseOptions) As CodeAnalysis.Compilation
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function VisualizeRealIL(peModule As CodeAnalysis.IModuleSymbol, methodData As CodeAnalysis.CodeGen.CompilationTestData.MethodData, markers As IReadOnlyDictionary(Of Integer, String)) As String
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
