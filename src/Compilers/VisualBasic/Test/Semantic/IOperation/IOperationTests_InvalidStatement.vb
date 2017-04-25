' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18077"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidVariableDeclarationStatement()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x, 1 As Integer'BIND:"Dim x, 1 As Integer"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x, 1 As Integer')
  IVariableDeclaration: x As System.Int32 (OperationKind.VariableDeclaration) (Syntax: 'x')
  IVariableDeclaration:  As System.Int32 (OperationKind.VariableDeclaration) (Syntax: '')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'x'.
        Dim x, 1 As Integer'BIND:"Dim x, 1 As Integer"
            ~
BC30203: Identifier expected.
        Dim x, 1 As Integer'BIND:"Dim x, 1 As Integer"
               ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidSwitchStatementExpression()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Select Case Program'BIND:"Select Case Program"
            Case 1
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
  Sections: ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'Case 1')
        Clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.Invalid) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: '1')
        Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Case 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30109: 'Program' is a class type and cannot be used as an expression.
        Select Case Program'BIND:"Select Case Program"
                    ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidSwitchStatementCaseLabel()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Select Case x.ToString()'BIND:"Select Case x.ToString()"
            Case x
                Exit Select
        End Select
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: IInvocationExpression (virtual Function System.Object.ToString() As System.String) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'x.ToString()')
      Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Sections: ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'Case x ... Exit Select')
        Clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.Invalid) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: 'x')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Case x ... Exit Select')
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program' cannot be converted to 'String'.
            Case x
                 ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidIfStatement()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        If x = Nothing Then'BIND:"If x = Nothing Then"
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If x = Noth ... End If')
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x = Nothing')
      IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'x = Nothing')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program, Constant: null) (Syntax: 'Nothing')
            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If x = Noth ... End If')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30452: Operator '=' is not defined for types 'Program' and 'Program'.
        If x = Nothing Then'BIND:"If x = Nothing Then"
           ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidIfElseIfStatement()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        If Then'BIND:"If Then"
        ElseIf x Then
            x
        Else
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'If Then'BIN ... Else')
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If Then'BIN ... Else')
  IfFalse: IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'ElseIf x Th ... x')
      Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x')
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'ElseIf x Th ... x')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x')
              Children(1): ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
      IfFalse: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Else')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30081: 'If' must end with a matching 'End If'.
        If Then'BIND:"If Then"
        ~~~~~~~
BC30201: Expression expected.
        If Then'BIND:"If Then"
           ~
BC30311: Value of type 'Program' cannot be converted to 'Boolean'.
        ElseIf x Then
               ~
BC30454: Expression is not a method.
            x
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_MissingConditionAndStep()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        For i As Integer = 0'BIND:"For i As Integer = 0"
        Next i
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For i As In ... Next i')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: '')
  Before: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: '0')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: '')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next i')
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next i')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next i')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next i')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30035: Syntax error.
        For i As Integer = 0'BIND:"For i As Integer = 0"
                            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_MissingConditionAndInitialization()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
        Next
    End Sub

    Private Function Method() As Integer
        Return 0
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For Step (M ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: '')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'For Step (M ... Next')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Object, Constant: 1) (Syntax: 'For Step (M ... Next')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.ObjectLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '')
          Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: '')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: '')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '')
          Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: '')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: '')
  Before: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object, IsInvalid) (Syntax: '')
        Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: '')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object, IsInvalid) (Syntax: '')
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: '')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: '')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For Step (M ... Next')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For Step (M ... Next')
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For Step (M ... Next')
      ICompoundAssignmentExpression (BinaryOperationKind.ObjectAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
        Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: '')
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'For Step (M ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For Step (M ... Next')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30035: Syntax error.
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
            ~
BC30035: Syntax error.
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
            ~
BC30201: Expression expected.
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
            ~
BC30249: '=' expected.
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_InvalidConditionAndStep()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        For i As Integer = 0 To Program Step x'BIND:"For i As Integer = 0 To Program Step x"
        Next i
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For i As In ... Next i')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'Program')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'x')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'Program')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'Program')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'Program')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'Program')
  Before: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: '0')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'Program')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program')
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'Program')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program')
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x')
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x')
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'x')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30108: 'Program' is a type and cannot be used as an expression.
        For i As Integer = 0 To Program Step x'BIND:"For i As Integer = 0 To Program Step x"
                                ~~~~~~~
BC30451: 'x' is not declared. It may be inaccessible due to its protection level.
        For i As Integer = 0 To Program Step x'BIND:"For i As Integer = 0 To Program Step x"
                                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidGotoStatement_MissingLabel()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Select Case args.Length
            Case 1
                GoTo Label1'BIND:"GoTo Label1"
        End Select
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'GoTo Label1')
  Children(1): IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: 'Label1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30132: Label 'Label1' is not defined.
                GoTo Label1'BIND:"GoTo Label1"
                     ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GoToStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidExitStatement_OutsideLoopOrSwitch()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Exit For'BIND:"Exit For"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'Exit For')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30096: 'Exit For' can only appear inside a 'For' statement.
        Exit For'BIND:"Exit For"
        ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ExitStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidContinueStatement_OutsideLoopOrSwitch()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Continue'BIND:"Continue"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'Continue')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
        Continue'BIND:"Continue"
        ~~~~~~~~
BC30781: 'Continue' must be followed by 'Do', 'For' or 'While'.
        Continue'BIND:"Continue"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ContinueStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18225"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidCaseStatement_OutsideSwitchBlock()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Case 0'BIND:"Case 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'Case 0')
  Children(1): IOperation:  (OperationKind.None) (Syntax: 'Case 0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30072: 'Case' can only appear inside a 'Select Case' statement.
        Case 0'BIND:"Case 0"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CaseStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidElseIfStatement_NoPrecedingIfStatement()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        ElseIf args.Length = 0'BIND:"ElseIf args.Length = 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'ElseIf args.Length = 0')
  Children(1): IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'args.Length = 0')
      Left: IIndexedPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'args.Length')
          Instance Receiver: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String()) (Syntax: 'args')
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36005: 'ElseIf' must be preceded by a matching 'If' or 'ElseIf'.
        ElseIf args.Length = 0'BIND:"ElseIf args.Length = 0"
        ~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ElseIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
