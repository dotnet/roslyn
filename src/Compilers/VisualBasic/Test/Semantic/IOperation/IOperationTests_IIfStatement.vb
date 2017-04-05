' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIf()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer = 0
        Dim returnValue As Integer = -1
        If count > 0 Then returnValue = count'BIND:"If count > 0 Then returnValue = count"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementMultiLineIf()
            Dim source = <![CDATA[
Module Program		
     Sub Main(args As String())		
         Dim count As Integer = 0		
         Dim returnValue As Integer = 1
        If count > 0 Then'BIND:"If count > 0 Then"
            returnValue = count
        End If
    End Sub		
 End Module		
 
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIfAndElse()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer
        Dim data As Integer
        If count > 10 Then data = data + count Else data = data - count'BIND:"If count > 10 Then data = data + count Else data = data - count"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: data (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: data (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: data (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: data (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineIfAndElseNested()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If m > 10 Then If n > 20 Then returnValue = n'BIND:"If m > 10 Then If n > 20 Then returnValue = n"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfWithConditionEvaluationTrue()
            Dim source = <![CDATA[
Class P
    Private Sub M()
        Dim condition As Boolean = False
        If 1 = 1 Then'BIND:"If 1 = 1 Then"
            condition = True
        End If
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True)
      Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfWithConditionConstantFalse()
            Dim source = <![CDATA[
Class P
    Private Sub M()
        Dim condition As Boolean = True
        If False Then'BIND:"If False Then"
            condition = False
        End If
    End Sub
End Class


    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSingleLineWithOperator()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10 And n > 20) Then returnValue = n'BIND:"If (m > 10 And n > 20) Then returnValue = n"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.BooleanAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementMultiLineIfWithElse()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim count As Integer = 0
        Dim returnValue As Integer = -1
        If count > 0 Then'BIND:"If count > 0 Then"
            returnValue = count
        Else
            returnValue = -1
        End If
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: count (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: returnValue (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, Constant: -1)
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementSimpleIfNested1()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10) Then'BIND:"If (m > 10) Then"
            If (n > 20) Then
                Console.WriteLine("Result 1")
            End If
        Else
            Console.WriteLine("Result 2")
        End If
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
          IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result 2)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementIfNested2()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 12
        Dim n As Integer = 18
        Dim returnValue As Integer = -1
        If (m > 10) Then'BIND:"If (m > 10) Then"
            If (n > 20) Then
                Console.WriteLine("Result 1")
            Else
                Console.WriteLine("Result 2")
            End If
        End If
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
          IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result 2)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithMultipleCondition()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        Dim p As Integer = 5
        If (m >= n AndAlso m >= p) Then'BIND:"If (m >= n AndAlso m >= p) Then"
            Console.WriteLine("Nothing Is larger than m.")
        End If
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Nothing Is larger than m.)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseIfCondition()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then'BIND:"If (m > 20) Then"
            Console.WriteLine("Result1")
        ElseIf (n > 10) Then
            Console.WriteLine("Result2")
        Else
            Console.WriteLine("Result3")
        End If
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
  IIfStatement (OperationKind.IfStatement)
    Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
        IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result2)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result3)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseIfSingleLine()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        Dim n As Integer = 7
        If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3") End If'BIND:"If (m > 20) Then System.Console.WriteLine("Result1") Else If (n > 10) Then System.Console.WriteLine("Result2") Else System.Console.WriteLine("Result3")"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
          IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result2)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result3)
]]>.Value

            VerifyOperationTreeForTest(Of SingleLineIfStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithElseMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If (m > 20) Then'BIND:"If (m > 20) Then"
            Console.WriteLine("Result1")
        Else
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithConditionMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If () Then'BIND:"If () Then"
            Console.WriteLine("Result1")
        End If

    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: ?, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithStatementMissing()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim m As Integer = 9
        If (m = 9) Then'BIND:"If (m = 9) Then"
        Else
        End If

    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")>
        Public Sub IIfstatementWithFuncCall()
            Dim source = <![CDATA[
Module Module1
    Sub Main()
        If (True) Then'BIND:"If (True) Then"
            A()
        Else
            B()
        End If
    End Sub
    Function A() As String
        Return "A"
    End Function
    Function B() As String
        Return "B"
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement)
  Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean, Constant: True)
      ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Function Module1.A() As System.String) (OperationKind.InvocationExpression, Type: System.String)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Function Module1.B() As System.String) (OperationKind.InvocationExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

    End Class
End Namespace
