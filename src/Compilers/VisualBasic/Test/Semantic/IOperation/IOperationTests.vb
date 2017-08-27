' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub InvalidUserDefinedOperators()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2
    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator

    Public Shared Operator -(x As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator
End Class

Module Module1
    Sub Main() 
        Dim x, y As New B2()
        x = x + 10
        x = x + y
        x = -x
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 3)

            ' x = x + 10 fails semantic analysis and does not have an operator method, but the operands are available.

            Assert.Equal("x = x + 10", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatement).Expression
            Assert.Equal(expression1.Kind, OperationKind.AssignmentExpression)
            Dim assignment1 As IAssignmentExpression = DirectCast(expression1, IAssignmentExpression)
            Assert.Equal(assignment1.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add1 As IBinaryOperatorExpression = DirectCast(assignment1.Value, IBinaryOperatorExpression)
            Assert.Equal(add1.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.False(add1.UsesOperatorMethod)
            Assert.Null(add1.OperatorMethod)
            Dim left1 As IOperation = add1.LeftOperand
            Assert.Equal(left1.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(left1, ILocalReferenceExpression).Local.Name, "x")
            Dim right1 As IOperation = add1.RightOperand
            Assert.Equal(right1.Kind, OperationKind.LiteralExpression)
            Dim literal1 As ILiteralExpression = DirectCast(right1, ILiteralExpression)
            Assert.Equal(CInt(literal1.ConstantValue.Value), 10)

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x = x + 10')
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2, IsInvalid) (Syntax: 'x = x + 10')
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
    Right: IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperationKind.BinaryOperatorExpression, Type: B2, IsInvalid) (Syntax: 'x + 10')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
")

            ' x = x + y passes semantic analysis.

            Assert.Equal("x = x + y", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatement).Expression
            Assert.Equal(expression2.Kind, OperationKind.AssignmentExpression)
            Dim assignment2 As IAssignmentExpression = DirectCast(expression2, IAssignmentExpression)
            Assert.Equal(assignment2.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add2 As IBinaryOperatorExpression = DirectCast(assignment2.Value, IBinaryOperatorExpression)
            Assert.Equal(add2.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.True(add2.UsesOperatorMethod)
            Assert.NotNull(add2.OperatorMethod)
            Assert.Equal(add2.OperatorMethod.Name, "op_Addition")
            Dim left2 As IOperation = add2.LeftOperand
            Assert.Equal(left2.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(left2, ILocalReferenceExpression).Local.Name, "x")
            Dim right2 As IOperation = add2.RightOperand
            Assert.Equal(right2.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(right2, ILocalReferenceExpression).Local.Name, "y")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x = x + y')
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2) (Syntax: 'x = x + y')
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
    Right: IBinaryOperatorExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x + y')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
        Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
")

            ' -x fails semantic analysis and does not have an operator method, but the operand is available.

            Assert.Equal("x = -x", nodes(2).ToString())
            Dim statement3 As IOperation = model.GetOperation(nodes(2))
            Assert.Equal(statement3.Kind, OperationKind.ExpressionStatement)
            Dim expression3 As IOperation = DirectCast(statement3, IExpressionStatement).Expression
            Assert.Equal(expression3.Kind, OperationKind.AssignmentExpression)
            Dim assignment3 As IAssignmentExpression = DirectCast(expression3, IAssignmentExpression)
            Assert.Equal(assignment3.Value.Kind, OperationKind.UnaryOperatorExpression)
            Dim negate3 As IUnaryOperatorExpression = DirectCast(assignment3.Value, IUnaryOperatorExpression)
            Assert.Equal(negate3.UnaryOperationKind, UnaryOperationKind.OperatorMethodMinus)
            Assert.False(negate3.UsesOperatorMethod)
            Assert.Null(negate3.OperatorMethod)
            Dim operand3 As IOperation = negate3.Operand
            Assert.Equal(operand3.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(operand3, ILocalReferenceExpression).Local.Name, "x")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x = -x')
  IAssignmentExpression (OperationKind.AssignmentExpression, Type: B2, IsInvalid) (Syntax: 'x = -x')
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
    Right: IUnaryOperatorExpression (UnaryOperationKind.OperatorMethodMinus) (OperationKind.UnaryOperatorExpression, Type: B2, IsInvalid) (Syntax: '-x')
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
")
        End Sub

        <Fact>
        Public Sub SimpleCompoundAssignment()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Class B2
    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator
End Class

Module Module1
    Sub Main()
        Dim x, y As Integer 
        Dim a, b As New B2()
        x += y
        a += b
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 2)

            ' x += y produces a compound assignment with an integer add.

            Assert.Equal("x += y", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatement).Expression
            Assert.Equal(expression1.Kind, OperationKind.CompoundAssignmentExpression)
            Dim assignment1 As ICompoundAssignmentExpression = DirectCast(expression1, ICompoundAssignmentExpression)
            Dim target1 As ILocalReferenceExpression = TryCast(assignment1.Target, ILocalReferenceExpression)
            Assert.NotNull(target1)
            Assert.Equal(target1.Local.Name, "x")
            Dim value1 As ILocalReferenceExpression = TryCast(assignment1.Value, ILocalReferenceExpression)
            Assert.NotNull(value1)
            Assert.Equal(value1.Local.Name, "y")
            Assert.Equal(assignment1.BinaryOperationKind, BinaryOperationKind.IntegerAdd)
            Assert.False(assignment1.UsesOperatorMethod)
            Assert.Null(assignment1.OperatorMethod)

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x += y')
  ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'x += y')
    Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
    Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
")

            ' a += b produces a compound assignment with an operator method add.

            Assert.Equal("a += b", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatement).Expression
            Assert.Equal(expression2.Kind, OperationKind.CompoundAssignmentExpression)
            Dim assignment2 As ICompoundAssignmentExpression = DirectCast(expression2, ICompoundAssignmentExpression)
            Dim target2 As ILocalReferenceExpression = TryCast(assignment2.Target, ILocalReferenceExpression)
            Assert.NotNull(target2)
            Assert.Equal(target2.Local.Name, "a")
            Dim value2 As ILocalReferenceExpression = TryCast(assignment2.Value, ILocalReferenceExpression)
            Assert.NotNull(value2)
            Assert.Equal(value2.Local.Name, "b")
            Assert.Equal(assignment2.BinaryOperationKind, BinaryOperationKind.OperatorMethodAdd)
            Assert.True(assignment2.UsesOperatorMethod)
            Assert.NotNull(assignment2.OperatorMethod)
            Assert.Equal(assignment2.OperatorMethod.Name, "op_Addition")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'a += b')
  ICompoundAssignmentExpression (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.CompoundAssignmentExpression, Type: B2) (Syntax: 'a += b')
    Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'a')
    Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'b')
")
        End Sub

        <Fact>
        Public Sub VerifyOperationTree_IfStatement()
            Dim source = <![CDATA[
Class C
    Sub Goo(x As Integer)
        If x <> 0 Then'BIND:"If x <> 0 Then"
            System.Console.Write(x)
        End If
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If x <> 0 T ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <> 0')
      Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If x <> 0 T ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(x)')
        IInvocationExpression (static Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub VerifyOperationTree_ForStatement()
            Dim source = <![CDATA[
Class C
    Sub Goo()
        For i = 0 To 10'BIND:"For i = 0 To 10"
            System.Console.Write(i)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i = 0 T ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32) (Syntax: '0')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i = 0 T ... Next')
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i = 0 T ... Next')
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i = 0 T ... Next')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i = 0 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i = 0 T ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(i)')
        IInvocationExpression (static Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(i)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        <WorkItem(382240, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=382240")>
        Public Sub NothingOrAddressOfInPlaceOfParamArray()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Module Module1
    Sub Main() 
        Test1(Nothing)
        Test2(New System.Guid(), Nothing)
        Test1(AddressOf Main)
        Test2(New System.Guid(), AddressOf Main)
    End Sub

    Sub Test1(ParamArray x as Integer())
    End Sub

    Sub Test2(y As Integer, ParamArray x as Integer())
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.RegularWithIOperationFeature)
            Dim tree = comp.SyntaxTrees.Single()

            comp.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        Test2(New System.Guid(), Nothing)
              ~~~~~~~~~~~~~~~~~
BC30581: 'AddressOf' expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        Test1(AddressOf Main)
              ~~~~~~~~~~~~~~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        Test2(New System.Guid(), AddressOf Main)
              ~~~~~~~~~~~~~~~~~
BC30581: 'AddressOf' expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        Test2(New System.Guid(), AddressOf Main)
                                 ~~~~~~~~~~~~~~
</expected>)

            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().ToArray()

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:=
"IInvocationExpression (static Sub Module1.Test1(ParamArray x As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Test1(Nothing)')
  Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'Nothing')
      IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32(), Constant: null) (Syntax: 'Nothing')
        ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... ), Nothing)')
  Children(3): IOperation:  (OperationKind.None) (Syntax: 'Test2')
    IObjectCreationExpression (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Guid) (Syntax: 'New System.Guid()')
    ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test1(AddressOf Main)')
  Children(2): IOperation:  (OperationKind.None) (Syntax: 'Test1')
    IOperation:  (OperationKind.None) (Syntax: 'AddressOf Main')")

            comp.VerifyOperationTree(nodes(3), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... essOf Main)')
  Children(3): IOperation:  (OperationKind.None) (Syntax: 'Test2')
    IObjectCreationExpression (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Guid) (Syntax: 'New System.Guid()')
    IOperation:  (OperationKind.None) (Syntax: 'AddressOf Main')")
        End Sub
    End Class
End Namespace
