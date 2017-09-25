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
IDoLoopStatement (DoLoopKind: DoWhileBottomLoop) (LoopKind.Do) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... While i < 4')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i < 4')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4, Language: Visual Basic) (Syntax: '4')
  IgnoredCondition: 
  null
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... While i < 4')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'sum += ids(i)')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'sum += ids(i)')
            Left: 
              ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'sum')
            Right: 
              IArrayElementReferenceExpression (OperationKind.None, Language: Visual Basic) (Syntax: 'ids(i)')
                Array reference: 
                  ILocalReferenceExpression: ids (OperationKind.LocalReferenceExpression, Type: System.Int32(), Language: Visual Basic) (Syntax: 'ids')
                Indices(1):
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoUntilLoopTest()
            Dim source = <![CDATA[
Class C
    Private X As Integer = 10
    Sub M(c As C)
        Do Until X < 0'BIND:"Do Until X < 0"
            X = X - 1
        Loop
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDoLoopStatement (DoLoopKind: DoUntilTopLoop) (LoopKind.Do) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'Do Until X  ... Loop')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'X < 0')
      Left: 
        IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
          Instance Receiver: 
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  IgnoredCondition: 
  null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Do Until X  ... Loop')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'X = X - 1')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X = X - 1')
            Left: 
              IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
            Right: 
              IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X - 1')
                Left: 
                  IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
                    Instance Receiver: 
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
                Right: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While condi ... End While')
  Condition: 
    ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition')
  Body: 
    IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While condi ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: 
            IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: 
              null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'value > 10')
            Left: 
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'condition = False')
              Expression: 
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition = False')
                  Left: 
                    ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition')
                  Right: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False, Language: Visual Basic) (Syntax: 'False')
        IfFalse: 
        null
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While i < 5 ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5, Language: Visual Basic) (Syntax: '5')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While i < 5 ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'sum += i')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'sum += i')
            Left: 
              ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'sum')
            Right: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
  Condition: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
  Body: 
    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: 
            IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: 
              null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'value > 5')
            Left: 
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5, Language: Visual Basic) (Syntax: '5')
        IfTrue: 
          IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... oop break")')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... oop break")')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"While-loop break"')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop break", Language: Visual Basic) (Syntax: '"While-loop break"')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'Exit While')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... statement")')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... statement")')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"While-loop statement"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop statement", Language: Visual Basic) (Syntax: '"While-loop statement"')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
  Condition: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
  Body: 
    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: 
            IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: 
              null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'value > 100')
            Left: 
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100, Language: Visual Basic) (Syntax: '100')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Throw New S ... occurred.")')
              Expression: 
                IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'Throw New S ... occurred.")')
                  IObjectCreationExpression (Constructor: Sub System.Exception..ctor(message As System.String)) (OperationKind.ObjectCreationExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'New System. ... occurred.")')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"An excepti ...  occurred."')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "An exception has occurred.", Language: Visual Basic) (Syntax: '"An excepti ...  occurred."')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: 
                    null
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... statement")')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... statement")')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"While-loop statement"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While-loop statement", Language: Visual Basic) (Syntax: '"While-loop statement"')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While (Inli ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: '(InlineAssi ... alue)) >= 0')
      Left: 
        IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '(InlineAssi ... (i, value))')
          Operand: 
            IInvocationExpression (Function Program.InlineAssignHelper(Of System.Int32)(ByRef target As System.Int32, value As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'InlineAssig ... r(i, value)')
              Instance Receiver: 
              null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: target) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'value')
                    ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While (Inli ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... , i, value)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... , i, value)')
            Instance Receiver: 
            null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"While {0} {1}"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "While {0} {1}", Language: Visual Basic) (Syntax: '"While {0} {1}"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'i')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'value')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'value -= 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value -= 1')
            Left: 
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While numbe ... End While')
  Condition: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'number')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While numbe ... End While')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
  Condition: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If (number  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: '(number Mod 2) = 0')
            Left: 
              IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '(number Mod 2)')
                Operand: 
                  IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number Mod 2')
                    Left: 
                      IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, Language: Visual Basic) (Syntax: '2')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If (number  ... End If')
            IReturnStatement (OperationKind.ReturnStatement, Language: Visual Basic) (Syntax: 'Return number')
              ReturnedValue: 
                IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'number += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number += 1')
            Left: 
              IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
]]>.Value

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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
  Condition: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
  Body: 
    IBlockStatement (4 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While True' ... End While')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If (number  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: '(number Mod 2) = 0')
            Left: 
              IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '(number Mod 2)')
                Operand: 
                  IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number Mod 2')
                    Left: 
                      IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, Language: Visual Basic) (Syntax: '2')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If (number  ... End If')
            IBranchStatement (BranchKind.GoTo, Label: Even) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'GoTo Even')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'number += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number += 1')
            Left: 
              IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
      ILabeledStatement (Label: Even) (OperationKind.LabeledStatement, Language: Visual Basic) (Syntax: 'Even:')
        Statement: 
        null
      IReturnStatement (OperationKind.ReturnStatement, Language: Visual Basic) (Syntax: 'Return number')
        ReturnedValue: 
          IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'number')
]]>.Value

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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, IsInvalid, Language: Visual Basic) (Syntax: 'While 'BIND ... End While')
  Condition: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, Language: Visual Basic) (Syntax: '')
      Children(0)
  Body: 
    IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid, Language: Visual Basic) (Syntax: 'While 'BIND ... End While')
      Locals: Local_1: value As System.Int32
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim value A ... ment(index)')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'value')
          Variables: Local_1: value As System.Int32
          Initializer: 
            IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'System.Thre ... ment(index)')
              Instance Receiver: 
              null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'index')
                    ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'index')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'value > 100')
            Left: 
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'value')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100, Language: Visual Basic) (Syntax: '100')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If value >  ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'condition = False')
              Expression: 
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition = False')
                  Left: 
                    ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition')
                  Right: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False, Language: Visual Basic) (Syntax: 'False')
        IfFalse: 
        null
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While (cond ... End While')
  Condition: 
    IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: '(condition)')
      Operand: 
        ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'condition')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While (cond ... End While')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While i <=  ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i <= 10')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
  Body: 
    IBlockStatement (3 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While i <=  ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If i < 9 Th ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i < 9')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9, Language: Visual Basic) (Syntax: '9')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If i < 9 Th ... End If')
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'Continue While')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While IsTru ... End While')
  Condition: 
    IInvocationExpression (Function ContinueTest.IsTrue(i As System.Int32) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'IsTrue(i)')
      Instance Receiver: 
      null
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While IsTru ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While i < 1 ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
  Body: 
    IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While i < 1 ... End While')
      Locals: Local_1: j As System.Int32
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim j As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'j')
          Variables: Local_1: j As System.Int32
          Initializer: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
      IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While j < 1 ... End While')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
        Body: 
          IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While j < 1 ... End While')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'j += 1')
              Expression: 
                ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j += 1')
                  Left: 
                    ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
                  Right: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(j)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'j')
                        ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While i < 1 ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
  Body: 
    IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While i < 1 ... End While')
      Locals: Local_1: j As System.Int32
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: Visual Basic) (Syntax: 'Dim j As Integer = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: Visual Basic) (Syntax: 'j')
          Variables: Local_1: j As System.Int32
          Initializer: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
      IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While j < 1 ... End While')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, Language: Visual Basic) (Syntax: '10')
        Body: 
          IBlockStatement (3 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While j < 1 ... End While')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'j += 1')
              Expression: 
                ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j += 1')
                  Left: 
                    ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
                  Right: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i = i + j')
              Expression: 
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i = i + j')
                  Left: 
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                  Right: 
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i + j')
                      Left: 
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                      Right: 
                        ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(j)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'j')
                        ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While Syste ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'System.Thre ... ment(i) < 5')
      Left: 
        IInvocationExpression (Function System.Threading.Interlocked.Increment(ByRef location As System.Int32) As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'System.Thre ... ncrement(i)')
          Instance Receiver: 
          null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: location) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5, Language: Visual Basic) (Syntax: '5')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While Syste ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While i > 0 ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i > 0')
      Left: 
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While i > 0 ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i += 1')
        Expression: 
          ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i += 1')
            Left: 
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While b = b ... End While')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'b = b')
      Left: 
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'b')
      Right: 
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'b')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While b = b ... End While')
      IReturnStatement (OperationKind.ReturnStatement, Language: Visual Basic) (Syntax: 'Return b')
        ReturnedValue: 
          ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'b')
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, IsInvalid, Language: Visual Basic) (Syntax: 'While Syste ... End While')
  Condition: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid, Language: Visual Basic) (Syntax: 'System.Math ...  x + 1) > 0')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid, Language: Visual Basic) (Syntax: 'System.Math ...  x + 1) > 0')
          Left: 
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, Language: Visual Basic) (Syntax: 'System.Math ... (x), x + 1)')
              Children(3):
                  IOperation:  (OperationKind.None, Language: Visual Basic) (Syntax: 'System.Math.Max')
                    Children(1):
                        IOperation:  (OperationKind.None, Language: Visual Basic) (Syntax: 'System.Math')
                  IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, Language: Visual Basic) (Syntax: 'System.Thre ... ecrement(x)')
                    Children(2):
                        IOperation:  (OperationKind.None, IsInvalid, Language: Visual Basic) (Syntax: 'System.Thre ... d.Decrement')
                          Children(1):
                              IOperation:  (OperationKind.None, Language: Visual Basic) (Syntax: 'System.Thre ... Interlocked')
                        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'x')
                  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'x + 1')
                    Left: 
                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'x')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'x')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
          Right: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: Visual Basic) (Syntax: 'While Syste ... End While')
      ITryStatement (OperationKind.None, Language: Visual Basic) (Syntax: 'Try ... End Try')
        Body: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Try ... End Try')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'y = CSByte(x / 2)')
              Expression: 
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'y = CSByte(x / 2)')
                  Left: 
                    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'y')
                  Right: 
                    IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'CSByte(x / 2)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Double, Language: Visual Basic) (Syntax: 'x / 2')
                          Left: 
                            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Language: Visual Basic) (Syntax: 'x')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte, Language: Visual Basic) (Syntax: 'x')
                          Right: 
                            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2, Language: Visual Basic) (Syntax: '2')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, Language: Visual Basic) (Syntax: '2')
        Catch clauses(0)
        Finally: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Finally ... Exception()')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Throw New S ... Exception()')
              Expression: 
                IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'Throw New S ... Exception()')
                  IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'New System.Exception()')
                    Arguments(0)
                    Initializer: 
                    null
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
IDoLoopStatement (DoLoopKind: DoWhileTopLoop) (LoopKind.Do) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'Do While G( ... Loop')
  Condition: 
    IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'G()')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'G')
      Arguments(0)
  IgnoredCondition: 
  null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Do While G( ... Loop')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.WriteLine(1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.WriteLine(1)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
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
IDoLoopStatement (DoLoopKind: DoWhileBottomLoop) (LoopKind.Do) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... p While G()')
  Condition: 
    IInvocationExpression ( Function C.G() As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'G()')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'G')
      Arguments(0)
  IgnoredCondition: 
  null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... p While G()')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.WriteLine(1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.WriteLine(1)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoLoopUntilStatement()
            Dim source = <![CDATA[
Class C
    Private X As Integer = 10
    Sub M(c As C)
        Do'BIND:"Do"
            X = X - 1
        Loop Until X < 0
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDoLoopStatement (DoLoopKind: DoUntilBottomLoop) (LoopKind.Do) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... Until X < 0')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'X < 0')
      Left: 
        IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
          Instance Receiver: 
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  IgnoredCondition: 
  null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Do'BIND:"Do ... Until X < 0')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'X = X - 1')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X = X - 1')
            Left: 
              IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
            Right: 
              IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X - 1')
                Left: 
                  IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
                    Instance Receiver: 
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
                Right: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IWhileLoopStatement (LoopKind.While) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'While Not b ... End While')
  Condition: 
    IUnaryOperatorExpression (UnaryOperatorKind.Not, Checked) (OperationKind.UnaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'Not breakLoop')
      Operand: 
        ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'breakLoop')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'While Not b ... End While')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.Wri ... te {0}", x)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(format As System.String, arg0 As System.Object)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.Wri ... te {0}", x)')
            Instance Receiver: 
            null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"Iterate {0}"')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Iterate {0}", Language: Visual Basic) (Syntax: '"Iterate {0}"')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'x')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'breakLoop = True')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'breakLoop = True')
            Left: 
              ILocalReferenceExpression: breakLoop (OperationKind.LocalReferenceExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'breakLoop')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
]]>.Value
            VerifyOperationTreeForTest(Of WhileBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IWhileUntilLoopStatement_DoWhileWithTopAndBottomCondition()
            Dim source = <![CDATA[
Class C
    Sub M(i As Integer)
        Do While i > 0'BIND:"Do While i > 0"
            i = i + 1
        Loop Until i <= 0

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDoLoopStatement (DoLoopKind: Invalid) (LoopKind.Do) (OperationKind.LoopStatement, IsInvalid, Language: Visual Basic) (Syntax: 'Do While i  ... ntil i <= 0')
  Condition: 
    IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i > 0')
      Left: 
        IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  IgnoredCondition: 
    IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'i <= 0')
      Left: 
        IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
      Right: 
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, Language: Visual Basic) (Syntax: '0')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: Visual Basic) (Syntax: 'Do While i  ... ntil i <= 0')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i = i + 1')
            Left: 
              IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
            Right: 
              IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i + 1')
                Left: 
                  IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'i')
                Right: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, Language: Visual Basic) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30238: 'Loop' cannot have a condition if matching 'Do' has one.
        Loop Until i <= 0
             ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DoLoopBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
