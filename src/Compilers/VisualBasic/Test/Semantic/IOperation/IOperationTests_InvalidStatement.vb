' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidVariableDeclarationStatement()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x, 1 As Integer'BIND:"Dim x, 1 As Integer"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim x, 1 As Integer')
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'x, 1 As Integer')
    Declarators:
        IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol:  As System.Int32) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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

        <CompilerTrait(CompilerFeature.IOperation)>
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
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: 
    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'Case 1')
          Clauses:
              ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: '1')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Case 1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30109: 'Program' is a class type and cannot be used as an expression.
        Select Case Program'BIND:"Select Case Program"
                    ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null, IsInvalid) (Syntax: 'Select Case ... End Select')
  Switch expression: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program) (Syntax: 'x')
      Arguments(0)
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null, IsInvalid) (Syntax: 'Case x ... Exit Select')
          Clauses:
              ISingleValueCaseClauseOperation (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'x')
                Value: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
          Body:
              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Case x ... Exit Select')
                IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'Exit Select')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Program' cannot be converted to 'String'.
            Case x
                 ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SelectBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'If x = Noth ... End If')
  Condition: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x = Nothing')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'x = Nothing')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
          Right: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, Constant: null, IsInvalid, IsImplicit) (Syntax: 'Nothing')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'Nothing')
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'If x = Noth ... End If')
  WhenFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30452: Operator '=' is not defined for types 'Program' and 'Program'.
        If x = Nothing Then'BIND:"If x = Nothing Then"
           ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'If Then'BIN ... Else')
  Condition: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'If Then'BIN ... Else')
  WhenFalse: 
    IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'ElseIf x Th ... x')
      Condition: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
      WhenTrue: 
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'ElseIf x Th ... x')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x')
            Expression: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
                Children(1):
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
      WhenFalse: 
        IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: 'Else')
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

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For i As In ... Next i')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
  LimitValue: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For i As In ... Next i')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For i As In ... Next i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For i As In ... Next i')
  NextVariables(1):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30035: Syntax error.
        For i As Integer = 0'BIND:"For i As Integer = 0"
                            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For Step (M ... Next')
  Locals: Local_1:  As System.Object
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol:  As System.Object) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '')
      Initializer: 
        null
  InitialValue: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  LimitValue: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'For Step (M ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For Step (M ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For Step (M ... Next')
  NextVariables(0)
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

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For i As In ... Next i')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Program')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
          Children(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For i As In ... Next i')
  NextVariables(1):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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

        <CompilerTrait(CompilerFeature.IOperation)>
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
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'GoTo Label1')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Label1')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30132: Label 'Label1' is not defined.
                GoTo Label1'BIND:"GoTo Label1"
                     ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of GoToStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidExitStatement_OutsideLoopOrSwitch()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Exit For'BIND:"Exit For"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Exit For')
  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30096: 'Exit For' can only appear inside a 'For' statement.
        Exit For'BIND:"Exit For"
        ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ExitStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidContinueStatement_OutsideLoopOrSwitch()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Continue'BIND:"Continue"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Continue')
  Children(0)
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidCaseStatement_OutsideSwitchBlock()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Case 0'BIND:"Case 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case 0')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30072: 'Case' can only appear inside a 'Select Case' statement.
        Case 0'BIND:"Case 0"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CaseStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'ElseIf args.Length = 0')
  Children(1):
      IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean, IsInvalid) (Syntax: 'args.Length = 0')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'args.Length')
            Instance Receiver: 
              IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String(), IsInvalid) (Syntax: 'args')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36005: 'ElseIf' must be preceded by a matching 'If' or 'ElseIf'.
        ElseIf args.Length = 0'BIND:"ElseIf args.Length = 0"
        ~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ElseIfStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub InvalidStatementFlow_01()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())'BIND:"Sub Main(args As String())"
        Select Case args.Length
            Case 1
                GoTo Label1
        End Select
    End Sub
End Module
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30132: Label 'Label1' is not defined.
                GoTo Label1
                     ~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'args.Length')
          Value: 
            IPropertyReferenceOperation: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
              Instance Receiver: 
                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')

    Jump if False (Regular) to Block[B3]
        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean, IsImplicit) (Syntax: '1')
          Left: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'GoTo Label1')
          Children(0)

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
