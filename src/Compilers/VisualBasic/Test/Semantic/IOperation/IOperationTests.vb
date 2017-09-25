﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
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
            Assert.Equal(expression1.Kind, OperationKind.SimpleAssignmentExpression)
            Dim assignment1 As ISimpleAssignmentExpression = DirectCast(expression1, ISimpleAssignmentExpression)
            Assert.Equal(assignment1.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add1 As IBinaryOperatorExpression = DirectCast(assignment1.Value, IBinaryOperatorExpression)
            Assert.Equal(add1.OperatorKind, CodeAnalysis.Semantics.BinaryOperatorKind.Add)
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
  Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsInvalid) (Syntax: 'x = x + 10')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
      Right: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: B2, IsInvalid) (Syntax: 'x + 10')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
")

            ' x = x + y passes semantic analysis.

            Assert.Equal("x = x + y", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatement).Expression
            Assert.Equal(expression2.Kind, OperationKind.SimpleAssignmentExpression)
            Dim assignment2 As ISimpleAssignmentExpression = DirectCast(expression2, ISimpleAssignmentExpression)
            Assert.Equal(assignment2.Value.Kind, OperationKind.BinaryOperatorExpression)
            Dim add2 As IBinaryOperatorExpression = DirectCast(assignment2.Value, IBinaryOperatorExpression)
            Assert.Equal(add2.OperatorKind, CodeAnalysis.Semantics.BinaryOperatorKind.Add)
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
  Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2) (Syntax: 'x = x + y')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
      Right: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.BinaryOperatorExpression, Type: B2) (Syntax: 'x + y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
          Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'y')
")

            ' -x fails semantic analysis and does not have an operator method, but the operand is available.

            Assert.Equal("x = -x", nodes(2).ToString())
            Dim statement3 As IOperation = model.GetOperation(nodes(2))
            Assert.Equal(statement3.Kind, OperationKind.ExpressionStatement)
            Dim expression3 As IOperation = DirectCast(statement3, IExpressionStatement).Expression
            Assert.Equal(expression3.Kind, OperationKind.SimpleAssignmentExpression)
            Dim assignment3 As ISimpleAssignmentExpression = DirectCast(expression3, ISimpleAssignmentExpression)
            Assert.Equal(assignment3.Value.Kind, OperationKind.UnaryOperatorExpression)
            Dim negate3 As IUnaryOperatorExpression = DirectCast(assignment3.Value, IUnaryOperatorExpression)
            Assert.Equal(negate3.OperatorKind, CodeAnalysis.Semantics.UnaryOperatorKind.Minus)
            Assert.False(negate3.UsesOperatorMethod)
            Assert.Null(negate3.OperatorMethod)
            Dim operand3 As IOperation = negate3.Operand
            Assert.Equal(operand3.Kind, OperationKind.LocalReferenceExpression)
            Assert.Equal(DirectCast(operand3, ILocalReferenceExpression).Local.Name, "x")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x = -x')
  Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: B2, IsInvalid) (Syntax: 'x = -x')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'x')
      Right: IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperationKind.UnaryOperatorExpression, Type: B2, IsInvalid) (Syntax: '-x')
          Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: B2, IsInvalid) (Syntax: 'x')
")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
            Assert.Equal(assignment1.OperatorKind, CodeAnalysis.Semantics.BinaryOperatorKind.Add)
            Assert.False(assignment1.UsesOperatorMethod)
            Assert.Null(assignment1.OperatorMethod)

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x += y')
  Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'x += y')
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
            Assert.Equal(assignment2.OperatorKind, CodeAnalysis.Semantics.BinaryOperatorKind.Add)
            Assert.True(assignment2.UsesOperatorMethod)
            Assert.NotNull(assignment2.OperatorMethod)
            Assert.Equal(assignment2.OperatorMethod.Name, "op_Addition")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'a += b')
  Expression: ICompoundAssignmentExpression (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.CompoundAssignmentExpression, Type: B2) (Syntax: 'a += b')
      Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'a')
      Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: B2) (Syntax: 'b')
")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyOperationTree_IfStatement()
            Dim source = <![CDATA[
Class C
    Sub F(x As Integer)
        If x <> 0 Then'BIND:"If x <> 0 Then"
            System.Console.Write(x)
        End If
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If x <> 0 T ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'x <> 0')
      Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If x <> 0 T ... End If')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(x)')
        Expression: IInvocationExpression (Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IfFalse: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyOperationTree_ForStatement()
            Dim source = <![CDATA[
Class C
    Sub F()
        For i = 0 To 10'BIND:"For i = 0 To 10"
            System.Console.Write(i)
        Next
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i = 0 T ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i = 0 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i = 0 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i = 0 T ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(i)')
        Expression: IInvocationExpression (Sub System.Console.Write(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
"IInvocationExpression (Sub Module1.Test1(ParamArray x As System.Int32())) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Test1(Nothing)')
  Instance Receiver: null
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'Nothing')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32(), Constant: null) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... ), Nothing)')
  Children(3):
      IOperation:  (OperationKind.None) (Syntax: 'Test2')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Module1) (Syntax: 'Test2')
      IObjectCreationExpression (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Guid, IsInvalid) (Syntax: 'New System.Guid()')
        Arguments(0)
        Initializer: null
      ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test1(AddressOf Main)')
  Children(2):
      IOperation:  (OperationKind.None) (Syntax: 'Test1')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Module1) (Syntax: 'Test1')
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf Main')
        Children(1):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Main')
              Children(1):
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Module1, IsInvalid) (Syntax: 'Main')")

            comp.VerifyOperationTree(nodes(3), expectedOperationTree:=
"IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... essOf Main)')
  Children(3):
      IOperation:  (OperationKind.None) (Syntax: 'Test2')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Module1) (Syntax: 'Test2')
      IObjectCreationExpression (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Guid, IsInvalid) (Syntax: 'New System.Guid()')
        Arguments(0)
        Initializer: null
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf Main')
        Children(1):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Main')
              Children(1):
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Module1, IsInvalid) (Syntax: 'Main')")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub BoundMethodGroup_ExposesReceiver()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim c1 As New C1
        Console.WriteLine(New With {Key .a = AddressOf c1.S})'BIND:"New With {Key .a = AddressOf c1.S}"
    End Sub

    Class C1
        Sub S()
        End Sub
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key a As ?>, IsInvalid) (Syntax: 'New With {K ... essOf c1.S}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'Key .a = AddressOf c1.S')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'AddressOf c1.S')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf c1.S')
                  Children(1):
                      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c1.S')
                        Children(1):
                            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Console.WriteLine(New With {Key .a = AddressOf c1.S})'BIND:"New With {Key .a = AddressOf c1.S}"
                                             ~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub BoundPropertyGroup_ExposesReceiver()
            Dim source = <![CDATA[
Option Strict Off

Class C
    Private Sub M(c As C, d As Object)
        Dim x = c(c, d)'BIND:"c(c, d)"
    End Sub

    Default ReadOnly Property P1(x As Integer, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String, x2 As Object) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'c(c, d)')
  Children(3):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'c')
        Children(1):
            IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30518: Overload resolution failed because no accessible 'P1' can be called with these arguments:
    'Public ReadOnly Default Property P1(x As Integer, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'Integer'.
    'Public ReadOnly Default Property P1(x As String, x2 As Object) As Integer': Value of type 'C' cannot be converted to 'String'.
        Dim x = c(c, d)'BIND:"c(c, d)"
                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestClone()
            Dim sourceCode = TestResource.AllInOneVisualBasicCode

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(sourceCode, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim model = compilation.GetSemanticModel(tree)

            VerifyClone(model)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestParentOperations()
            Dim sourceCode = TestResource.AllInOneVisualBasicCode

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(sourceCode, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim model = compilation.GetSemanticModel(tree)

            VerifyParentOperations(model)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestEndStatement()
            Dim source = <![CDATA[
Class C
    Public Shared i as Integer
    Public Shared Sub Main()
        If i = 0 Then
            End'BIND:"End"
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IEndStatement (OperationKind.None) (Syntax: 'End')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of StopOrEndStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, compilationOptions:=TestOptions.ReleaseExe)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestEndStatement_Parent()
            Dim source = <![CDATA[
Class C
    Public Shared i As Integer
    Public Shared Sub Main()
        If i = 0 Then'BIND:"If i = 0 Then"
            End
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If i = 0 Th ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i = 0')
      Left: IFieldReferenceExpression: C.i As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i')
          Instance Receiver: null
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If i = 0 Th ... End If')
      IEndStatement (OperationKind.None) (Syntax: 'End')
  IfFalse: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics, compilationOptions:=TestOptions.ReleaseExe)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestStopStatement()
            Dim source = <![CDATA[
Class C
    Public Shared i as Integer
    Public Shared Sub Main()
        If i = 0 Then
            Stop'BIND:"Stop"
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IStopStatement (OperationKind.None) (Syntax: 'Stop')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of StopOrEndStatementSyntax)(source, expectedOperationTree, expectedDiagnostics, compilationOptions:=TestOptions.ReleaseExe)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestStopStatement_Parent()
            Dim source = <![CDATA[
Class C
    Public Shared i As Integer
    Public Shared Sub Main()
        If i = 0 Then'BIND:"If i = 0 Then"
            Stop
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If i = 0 Th ... End If')
  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i = 0')
      Left: IFieldReferenceExpression: C.i As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i')
          Instance Receiver: null
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If i = 0 Th ... End If')
      IStopStatement (OperationKind.None) (Syntax: 'Stop')
  IfFalse: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics, compilationOptions:=TestOptions.ReleaseExe)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestCatchClause()
            Dim source = <![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Try
            Main(Nothing)
        Catch ex As Exception When ex Is Nothing'BIND:"Catch ex As Exception When ex Is Nothing"

        End Try
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ICatchClause (Exception type: System.Exception, Exception local: ex As System.Exception) (OperationKind.None) (Syntax: 'Catch ex As ...  Is Nothing')
  Filter: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'ex Is Nothing')
      Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'ex')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILocalReferenceExpression: ex (OperationKind.LocalReferenceExpression, Type: System.Exception) (Syntax: 'ex')
      Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
  Handler: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'Catch ex As ...  Is Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CatchBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


    End Class
End Namespace
