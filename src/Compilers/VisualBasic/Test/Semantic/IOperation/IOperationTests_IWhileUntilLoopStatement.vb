' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
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
IWhileUntilLoopStatement (IsTopTest: False, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32)
            ILocalReferenceExpression: ids (OperationKind.LocalReferenceExpression, Type: System.Int32())
            Indices: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: value As System.Int32
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: value As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression (static Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
            IArgument (Matching Parameter: location) (OperationKind.Argument)
              ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
            Right: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub
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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: value As System.Int32
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: value As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression (static Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
            IArgument (Matching Parameter: location) (OperationKind.Argument)
              ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop break)
        IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop statement)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: value As System.Int32
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: value As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression (static Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
            IArgument (Matching Parameter: location) (OperationKind.Argument)
              ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: Sub System.Exception..ctor(message As System.String)) (OperationKind.ObjectCreationExpression, Type: System.Exception)
            Arguments: IArgument (Matching Parameter: message) (OperationKind.Argument)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: An exception has occurred.)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop statement)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32)
          IInvocationExpression (static Function Program.InlineAssignHelper(Of System.Int32)(ByRef target As System.Int32, value As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
            IArgument (Matching Parameter: target) (OperationKind.Argument)
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: format) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While {0} {1})
        IArgument (Matching Parameter: arg0) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        IArgument (Matching Parameter: arg1) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean)
      ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32)
              IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IReturnStatement (OperationKind.ReturnStatement)
          IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (4 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32)
              IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.GoTo, Label: Even) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    ILabelStatement (Label: Even) (OperationKind.LabelStatement)
    IReturnStatement (OperationKind.ReturnStatement)
      IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: value As System.Int32
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: value As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression (static Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
            IArgument (Matching Parameter: location) (OperationKind.Argument)
              ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
            Right: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileMultipleCondition()
            Dim source = <![CDATA[
Class ContinueTest
    Private Shared Sub Main()
        Dim i As Integer = 0, j As Integer = 0
        While (i <= 10) AndAlso j <= 20'BIND:"While (i <= 10) AndAlso j <= 20"
            i += 1
            j = j * i
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
          IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (3 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value
            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileConditionInVocationExpression()
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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IInvocationExpression (static Function ContinueTest.IsTrue(i As System.Int32) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean)
      IArgument (Matching Parameter: i) (OperationKind.Argument)
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileBreakFromNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                If j > i Then
                    Exit While
                End If
                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileContinueFromNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                If j < i Then
                    Continue While
                End If
                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileGotoFromNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                If j > i Then
                    GoTo FirstLoop
                End If

                System.Console.WriteLine(j)
            End While
FirstLoop:
            Continue While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (6 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.GoTo, Label: FirstLoop) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    ILabelStatement (Label: FirstLoop) (OperationKind.LabelStatement)
    IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileThrowFromNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                If j > i Then
                    Throw New System.Exception("Exception Hit")
                End If

                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IThrowStatement (OperationKind.ThrowStatement)
              IObjectCreationExpression (Constructor: Sub System.Exception..ctor(message As System.String)) (OperationKind.ObjectCreationExpression, Type: System.Exception)
                Arguments: IArgument (Matching Parameter: message) (OperationKind.Argument)
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Exception Hit)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_WhileReturnFromNested()
            Dim source = <![CDATA[
Class Test
    Private Shared Sub Main()
        Dim i As Integer = 0
        While i < 10'BIND:"While i < 10"
            i += 1
            Dim j As Integer = 0
            While j < 10
                j += 1
                If j > i Then
                    Return
                End If

                System.Console.WriteLine(j)
            End While
            System.Console.WriteLine(i)
        End While
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: j As System.Int32
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: j As System.Int32 (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IReturnStatement (OperationKind.ReturnStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IInvocationExpression (static Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
          IArgument (Matching Parameter: location) (OperationKind.Argument)
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True)
      Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
      Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IReturnStatement (OperationKind.ReturnStatement)
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
        Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    ITryStatement (OperationKind.TryStatement)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.SByte)
            Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.SByte)
            Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.SByte)
                IBinaryOperatorExpression (BinaryOperationKind.FloatingDivide) (OperationKind.BinaryOperatorExpression, Type: System.Double)
                  Left: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double)
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte)
                  Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2)
                      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: False, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: C)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IUnaryOperatorExpression (UnaryOperationKind.BooleanBitwiseNegation) (OperationKind.UnaryOperatorExpression, Type: System.Boolean)
      ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(format As System.String, arg0 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: format) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Iterate {0})
        IArgument (Matching Parameter: arg0) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
]]>.Value

            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

    End Class
End Namespace

