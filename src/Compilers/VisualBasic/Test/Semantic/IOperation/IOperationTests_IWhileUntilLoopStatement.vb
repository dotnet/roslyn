' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoWhileLoopsTest()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim ids As Integer() = New Integer() {6, 7, 8, 10}
        Dim sum As Integer = 0
        Dim i As Integer = 0
        Do'BIND:"Do"
            sum += ids(i)
            i += 1
        Loop While i < 4

        System.Console.WriteLine(sum)
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: False, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'Do'BIND:"Do ... While i < 4')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 4')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'Do'BIND:"Do ... While i < 4')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'sum += ids(i)')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'sum += ids(i)')
            Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'sum')
            Right: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'ids(i)')
                Array reference: ILocalReferenceExpression: ids (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'ids')
                Indices(1):
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileConditionTrue()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True
        While condition'BIND:"While condition"
            Dim value As Integer = System.Threading.Interlocked.Increment(index)
            If value > 10 Then
                condition = False
            End If
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While condi ... End While')
  Condition: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
  Body: IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'While condi ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If value >  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'value > 10')
            Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = False')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = False')
                  Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
        IfFalse: null
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileLoopsTest()
            Dim source = <![CDATA[
Class Program
    Private Shared Function SumWhile() As Integer
        '
        ' Sum numbers 0 .. 4
        '
        Dim sum As Integer = 0
        Dim i As Integer = 0
        While i < 5'BIND:"While i < 5"
            sum += i
            i += 1
        End While
        Return sum
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While i < 5 ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While i < 5 ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'sum += i')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'sum += i')
            Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'sum')
            Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithBreak()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim index As Integer = 0
        While True'BIND:"While True"
            Dim value As Integer = System.Threading.Interlocked.Increment(index)
            If value > 5 Then
                System.Console.WriteLine("While-loop break")
                Exit While
            End If
            System.Console.WriteLine("While-loop statement")
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While True' ... End While')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  Body: IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'While True' ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If value >  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'value > 5')
            Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
        IfTrue: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... oop break")')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... oop break")')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"While-loop break"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop break") (Syntax: '"While-loop break"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit While')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... statement")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... statement")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"While-loop statement"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop statement") (Syntax: '"While-loop statement"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithThrow()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim index As Integer = 0
        While True'BIND:"While True"
            Dim value As Integer = System.Threading.Interlocked.Increment(index)
            If value > 100 Then
                Throw New System.Exception("An exception has occurred.")
            End If
            System.Console.WriteLine("While-loop statement")
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While True' ... End While')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  Body: IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'While True' ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If value >  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'value > 100')
            Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Throw New S ... occurred.")')
              Expression: IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'Throw New S ... occurred.")')
                  IObjectCreationExpression (Constructor: Sub System.Exception..ctor(message As System.String)) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'New System. ... occurred.")')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: '"An excepti ...  occurred."')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "An exception has occurred.") (Syntax: '"An excepti ...  occurred."')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: null
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... statement")')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... statement")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '"While-loop statement"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop statement") (Syntax: '"While-loop statement"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithAssignment()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim value As Integer = 4
        Dim i As Integer
        While (InlineAssignHelper(i, value)) >= 0'BIND:"While (InlineAssignHelper(i, value)) >= 0"
            System.Console.WriteLine("While {0} {1}", i, value)
            value -= 1
        End While
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While (Inli ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(InlineAssi ... alue)) >= 0')
      Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(InlineAssi ... (i, value))')
          Operand: IInvocationExpression (Function Program.InlineAssignHelper(Of System.Int32)(ByRef target As System.Int32, value As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'InlineAssig ... r(i, value)')
              Instance Receiver: null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: target) (OperationKind.Argument) (Syntax: 'i')
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'value')
                    ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While (Inli ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... , i, value)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... , i, value)')
            Instance Receiver: null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '"While {0} {1}"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While {0} {1}") (Syntax: '"While {0} {1}"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'i')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'value')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'value -= 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'value -= 1')
            Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileImplicit()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim number As Integer = 10
        While number'BIND:"While number"
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While numbe ... End While')
  Condition: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean) (Syntax: 'number')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'While numbe ... End While')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithReturn()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        System.Console.WriteLine(GetFirstEvenNumber(33))
    End Sub
    Public Shared Function GetFirstEvenNumber(number As Integer) As Integer
        While True'BIND:"While True"
            If (number Mod 2) = 0 Then
                Return number
            End If

            number += 1
        End While
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While True' ... End While')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While True' ... End While')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (number  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(number Mod 2) = 0')
            Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(number Mod 2)')
                Operand: IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'number Mod 2')
                    Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (number  ... End If')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return number')
              ReturnedValue: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'number += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'number += 1')
            Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithGoto()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        System.Console.WriteLine(GetFirstEvenNumber(33))
    End Sub
    Public Shared Function GetFirstEvenNumber(number As Integer) As Integer
        While True'BIND:"While True"
            If (number Mod 2) = 0 Then
                GoTo Even
            End If
            number += 1
Even:
            Return number
        End While
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While True' ... End While')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  Body: IBlockStatement (4 statements) (OperationKind.BlockStatement) (Syntax: 'While True' ... End While')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If (number  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(number Mod 2) = 0')
            Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(number Mod 2)')
                Operand: IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'number Mod 2')
                    Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If (number  ... End If')
            IBranchStatement (BranchKind.GoTo, Label: Even) (OperationKind.BranchStatement) (Syntax: 'GoTo Even')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'number += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'number += 1')
            Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILabeledStatement (Label: Even) (OperationKind.LabeledStatement) (Syntax: 'Even:')
        Statement: null
      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return number')
        ReturnedValue: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileMissingCondition()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True
        While 'BIND:"While "
            Dim value As Integer = System.Threading.Interlocked.Increment(index)
            If value > 100 Then
                condition = False
            End If
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'While 'BIND ... End While')
  Condition: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'While 'BIND ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If value >  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'value > 100')
            Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = False')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = False')
                  Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
        IfFalse: null
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileMissingStatement()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True
        While (condition)'BIND:"While (condition)"
        End While
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While (cond ... End While')
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean) (Syntax: '(condition)')
      Operand: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'While (cond ... End While')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithContinue()
            Dim source = <![CDATA[
Class ContinueTest
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i <= 10'BIND:"While i <= 10"
            i += 1
            If i < 9 Then
                Continue While
            End If
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While i <=  ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i <= 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'While i <=  ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If i < 9 Th ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 9')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If i < 9 Th ... End If')
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Continue While')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileConditionInvocationExpression()
            Dim source = <![CDATA[
Class ContinueTest
    Private Shared Sub Main()
        Dim i As Integer = 0
        While IsTrue(i)'BIND:"While IsTrue(i)"
            i += 1
            System.Console.WriteLine(i)
        End While
    End Sub
    Private Shared Function IsTrue(i As Integer) As Boolean
        If i < 9 Then
            Return True
        Else
            Return False
        End If
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While IsTru ... End While')
  Condition: IInvocationExpression (Function ContinueTest.IsTrue(i As System.Int32) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'IsTrue(i)')
      Instance Receiver: null
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument) (Syntax: 'i')
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While IsTru ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While i < 1 ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'While i < 1 ... End While')
      Locals: Local_1: j As System.Int32
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim j As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j')
          Variables: Local_1: j As System.Int32
          Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While j < 1 ... End While')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While j < 1 ... End While')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j += 1')
              Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'j += 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(j)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'j')
                        ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileChangeOuterInnerValue()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                i = i + j
                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While i < 1 ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'While i < 1 ... End While')
      Locals: Local_1: j As System.Int32
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim j As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j')
          Variables: Local_1: j As System.Int32
          Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While j < 1 ... End While')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Body: IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'While j < 1 ... End While')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j += 1')
              Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'j += 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + j')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + j')
                  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  Right: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + j')
                      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                      Right: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(j)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'j')
                        ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileIncrementInCondition()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim i As Integer = 0
        While System.Threading.Interlocked.Increment(i) < 5'BIND:"While System.Threading.Interlocked.Increment(i) < 5"
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While Syste ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'System.Thre ... ment(i) < 5')
      Left: IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'System.Thre ... ncrement(i)')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument) (Syntax: 'i')
                ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'While Syste ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileInfiniteLoop()
            Dim source = <![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim i As Integer = 1
        While i > 0'BIND:"While i > 0"
            i += 1
        End While
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While i > 0 ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 0')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'While i > 0 ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i += 1')
        Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'i += 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileConstantCheck()
            Dim source = <![CDATA[
Class Program
    Private Function foo() As Boolean
        Const b As Boolean = True
        While b = b'BIND:"While b = b"
            Return b
        End While
    End Function

End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While b = b ... End While')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True) (Syntax: 'b = b')
      Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
      Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'While b = b ... End While')
      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Return b')
        ReturnedValue: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithTryCatch()
            Dim source = <![CDATA[
Public Class TryCatchFinally
    Public Sub TryMethod()
        Dim x As SByte = 111, y As SByte
        While System.Math.Max(System.Threading.Interlocked.Decrement(x), x + 1) > 0'BIND:"While System.Math.Max(System.Threading.Interlocked.Decrement(x), x + 1) > 0"
            Try
                y = CSByte(x / 2)
            Finally
                Throw New System.Exception()
            End Try
        End While

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'While Syste ... End While')
  Condition: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'System.Math ...  x + 1) > 0')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'System.Math ...  x + 1) > 0')
          Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'System.Math ... (x), x + 1)')
              Children(3):
                  IOperation:  (OperationKind.None) (Syntax: 'System.Math.Max')
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'System.Thre ... ecrement(x)')
                    Children(2):
                        IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'System.Thre ... d.Decrement')
                        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'x')
                  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + 1')
                    Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'x')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'x')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'While Syste ... End While')
      ITryStatement (OperationKind.TryStatement) (Syntax: 'Try ... End Try')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Try ... End Try')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y = CSByte(x / 2)')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.SByte) (Syntax: 'y = CSByte(x / 2)')
                  Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'y')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte) (Syntax: 'CSByte(x / 2)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double) (Syntax: 'x / 2')
                          Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'x')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte) (Syntax: 'x')
                          Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2) (Syntax: '2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Catch clauses(0)
        Finally: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Finally ... Exception()')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Throw New S ... Exception()')
              Expression: IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception) (Syntax: 'Throw New S ... Exception()')
                  IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'New System.Exception()')
                    Arguments(0)
                    Initializer: null
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoWhileFuncCall()
            Dim source = <![CDATA[
Imports System

Class C
    Sub F()
        Do While G()'BIND:"Do While G()"
            Console.WriteLine(1)
        Loop
    End Sub

    Function G() As Boolean
        Return False
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'Do While G( ... Loop')
  Condition: IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'G()')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'G')
      Arguments(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do While G( ... Loop')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(1)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(1)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoLoopWhileStatement()
            Dim source = <![CDATA[
Imports System

Class C
    Sub F()
        Do'BIND:"Do"
            Console.WriteLine(1)
        Loop While G()
    End Sub

    Function G() As Boolean
        Return False
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: False, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'Do'BIND:"Do ... p While G()')
  Condition: IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'G()')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'G')
      Arguments(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Do'BIND:"Do ... p While G()')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(1)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(1)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileWithNot()
            Dim source = <![CDATA[
Imports System
Module M1
    Sub Main()
        Dim x As Integer
        Dim breakLoop As Boolean
        x = 1
        breakLoop = False
        While Not breakLoop'BIND:"While Not breakLoop"
            Console.WriteLine("Iterate {0}", x)
            breakLoop = True
        End While
    End Sub
End Module

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement) (Syntax: 'While Not b ... End While')
  Condition: IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'Not breakLoop')
      Operand: ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'breakLoop')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'While Not b ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... te {0}", x)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(format As System.String, arg0 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... te {0}", x)')
            Instance Receiver: null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '"Iterate {0}"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Iterate {0}") (Syntax: '"Iterate {0}"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'x')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'breakLoop = True')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'breakLoop = True')
            Left: ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'breakLoop')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
]]>.Value
            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
