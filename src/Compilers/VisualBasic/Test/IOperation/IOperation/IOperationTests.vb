' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    <CompilerTrait(CompilerFeature.IOperation)>
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

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 3)

            ' x = x + 10 fails semantic analysis and does not have an operator method, but the operands are available.

            Assert.Equal("x = x + 10", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatementOperation).Operation
            Assert.Equal(expression1.Kind, OperationKind.SimpleAssignment)
            Dim assignment1 As ISimpleAssignmentOperation = DirectCast(expression1, ISimpleAssignmentOperation)
            Assert.Equal(assignment1.Value.Kind, OperationKind.Binary)
            Dim add1 As IBinaryOperation = DirectCast(assignment1.Value, IBinaryOperation)
            Assert.Equal(add1.OperatorKind, Operations.BinaryOperatorKind.Add)
            Assert.Null(add1.OperatorMethod)
            Dim left1 As IOperation = add1.LeftOperand
            Assert.Equal(left1.Kind, OperationKind.LocalReference)
            Assert.Equal(DirectCast(left1, ILocalReferenceOperation).Local.Name, "x")
            Dim right1 As IOperation = add1.RightOperand
            Assert.Equal(right1.Kind, OperationKind.Literal)
            Dim literal1 As ILiteralOperation = DirectCast(right1, ILiteralOperation)
            Assert.Equal(CInt(literal1.ConstantValue.Value), 10)

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = x + 10')
  Expression: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsInvalid, IsImplicit) (Syntax: 'x = x + 10')
      Left: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
      Right: 
        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: B2, IsInvalid) (Syntax: 'x + 10')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')")

            ' x = x + y passes semantic analysis.

            Assert.Equal("x = x + y", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatementOperation).Operation
            Assert.Equal(expression2.Kind, OperationKind.SimpleAssignment)
            Dim assignment2 As ISimpleAssignmentOperation = DirectCast(expression2, ISimpleAssignmentOperation)
            Assert.Equal(assignment2.Value.Kind, OperationKind.Binary)
            Dim add2 As IBinaryOperation = DirectCast(assignment2.Value, IBinaryOperation)
            Assert.Equal(add2.OperatorKind, Operations.BinaryOperatorKind.Add)
            Assert.NotNull(add2.OperatorMethod)
            Assert.Equal(add2.OperatorMethod.Name, "op_Addition")
            Dim left2 As IOperation = add2.LeftOperand
            Assert.Equal(left2.Kind, OperationKind.LocalReference)
            Assert.Equal(DirectCast(left2, ILocalReferenceOperation).Local.Name, "x")
            Dim right2 As IOperation = add2.RightOperand
            Assert.Equal(right2.Kind, OperationKind.LocalReference)
            Assert.Equal(DirectCast(right2, ILocalReferenceOperation).Local.Name, "y")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = x + y')
  Expression: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsImplicit) (Syntax: 'x = x + y')
      Left: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
      Right: 
        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.Binary, Type: B2) (Syntax: 'x + y')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
          Right: 
            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: B2) (Syntax: 'y')")

            ' -x fails semantic analysis and does not have an operator method, but the operand is available.

            Assert.Equal("x = -x", nodes(2).ToString())
            Dim statement3 As IOperation = model.GetOperation(nodes(2))
            Assert.Equal(statement3.Kind, OperationKind.ExpressionStatement)
            Dim expression3 As IOperation = DirectCast(statement3, IExpressionStatementOperation).Operation
            Assert.Equal(expression3.Kind, OperationKind.SimpleAssignment)
            Dim assignment3 As ISimpleAssignmentOperation = DirectCast(expression3, ISimpleAssignmentOperation)
            Assert.Equal(assignment3.Value.Kind, OperationKind.Unary)
            Dim negate3 As IUnaryOperation = DirectCast(assignment3.Value, IUnaryOperation)
            Assert.Equal(negate3.OperatorKind, Operations.UnaryOperatorKind.Minus)
            Assert.Null(negate3.OperatorMethod)
            Dim operand3 As IOperation = negate3.Operand
            Assert.Equal(operand3.Kind, OperationKind.LocalReference)
            Assert.Equal(DirectCast(operand3, ILocalReferenceOperation).Local.Name, "x")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:="
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = -x')
  Expression: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B2, IsInvalid, IsImplicit) (Syntax: 'x = -x')
      Left: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2) (Syntax: 'x')
      Right: 
        IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.Unary, Type: B2, IsInvalid) (Syntax: '-x')
          Operand: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: B2, IsInvalid) (Syntax: 'x')")
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

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).ToArray()
            Assert.Equal(nodes.Length, 2)

            ' x += y produces a compound assignment with an integer add.

            Assert.Equal("x += y", nodes(0).ToString())
            Dim statement1 As IOperation = model.GetOperation(nodes(0))
            Assert.Equal(statement1.Kind, OperationKind.ExpressionStatement)
            Dim expression1 As IOperation = DirectCast(statement1, IExpressionStatementOperation).Operation
            Assert.Equal(expression1.Kind, OperationKind.CompoundAssignment)
            Dim assignment1 As ICompoundAssignmentOperation = DirectCast(expression1, ICompoundAssignmentOperation)
            Dim target1 As ILocalReferenceOperation = TryCast(assignment1.Target, ILocalReferenceOperation)
            Assert.NotNull(target1)
            Assert.Equal(target1.Local.Name, "x")
            Dim value1 As ILocalReferenceOperation = TryCast(assignment1.Value, ILocalReferenceOperation)
            Assert.NotNull(value1)
            Assert.Equal(value1.Local.Name, "y")
            Assert.Equal(assignment1.OperatorKind, Operations.BinaryOperatorKind.Add)
            Assert.Null(assignment1.OperatorMethod)

            comp.VerifyOperationTree(nodes(0), expectedOperationTree:="
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x += y')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperationKind.CompoundAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x += y')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
      Right: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')")

            ' a += b produces a compound assignment with an operator method add.

            Assert.Equal("a += b", nodes(1).ToString())
            Dim statement2 As IOperation = model.GetOperation(nodes(1))
            Assert.Equal(statement2.Kind, OperationKind.ExpressionStatement)
            Dim expression2 As IOperation = DirectCast(statement2, IExpressionStatementOperation).Operation
            Assert.Equal(expression2.Kind, OperationKind.CompoundAssignment)
            Dim assignment2 As ICompoundAssignmentOperation = DirectCast(expression2, ICompoundAssignmentOperation)
            Dim target2 As ILocalReferenceOperation = TryCast(assignment2.Target, ILocalReferenceOperation)
            Assert.NotNull(target2)
            Assert.Equal(target2.Local.Name, "a")
            Dim value2 As ILocalReferenceOperation = TryCast(assignment2.Value, ILocalReferenceOperation)
            Assert.NotNull(value2)
            Assert.Equal(value2.Local.Name, "b")
            Assert.Equal(assignment2.OperatorKind, Operations.BinaryOperatorKind.Add)
            Assert.NotNull(assignment2.OperatorMethod)
            Assert.Equal(assignment2.OperatorMethod.Name, "op_Addition")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:="
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a += b')
  Expression: 
    ICompoundAssignmentOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function B2.op_Addition(x As B2, y As B2) As B2) (OperationKind.CompoundAssignment, Type: B2, IsImplicit) (Syntax: 'a += b')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Left: 
        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: B2) (Syntax: 'a')
      Right: 
        ILocalReferenceOperation: b (OperationKind.LocalReference, Type: B2) (Syntax: 'b')")
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
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If x <> 0 T ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.NotEquals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x <> 0')
      Left: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If x <> 0 T ... End If')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(x)')
        Expression: 
          IInvocationOperation (Sub System.Console.Write(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  WhenFalse: 
    null
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For i = 0 T ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i = 0 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i = 0 T ... Next')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i = 0 T ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(i)')
        Expression: 
          IInvocationOperation (Sub System.Console.Write(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
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

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
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
"IInvocationOperation (Sub Module1.Test1(ParamArray x As System.Int32())) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test1(Nothing)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)")

            comp.VerifyOperationTree(nodes(1), expectedOperationTree:=
"IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... ), Nothing)')
  Children(3):
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'Test2')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Module1, IsImplicit) (Syntax: 'Test2')
      IObjectCreationOperation (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreation, Type: System.Guid, IsInvalid) (Syntax: 'New System.Guid()')
        Arguments(0)
        Initializer: 
          null
      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')")

            comp.VerifyOperationTree(nodes(2), expectedOperationTree:=
"IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Test1(AddressOf Main)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'Test1')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Module1, IsImplicit) (Syntax: 'Test1')
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Main')
        Children(1):
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Main')
              Children(1):
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Module1, IsInvalid, IsImplicit) (Syntax: 'Main')")

            comp.VerifyOperationTree(nodes(3), expectedOperationTree:=
"IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Test2(New S ... essOf Main)')
  Children(3):
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'Test2')
        Children(1):
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Module1, IsImplicit) (Syntax: 'Test2')
      IObjectCreationOperation (Constructor: Sub System.Guid..ctor()) (OperationKind.ObjectCreation, Type: System.Guid, IsInvalid) (Syntax: 'New System.Guid()')
        Arguments(0)
        Initializer: 
          null
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf Main')
        Children(1):
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Main')
              Children(1):
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Module1, IsInvalid, IsImplicit) (Syntax: 'Main')")
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
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As ?>, IsInvalid) (Syntax: 'New With {K ... essOf c1.S}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Key .a = AddressOf c1.S')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key a As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {K ... essOf c1.S}')
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'AddressOf c1.S')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf c1.S')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'c1.S')
                        Children(1):
                            IOperation:  (OperationKind.None, Type: Program.C1, IsInvalid) (Syntax: 'c1')
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
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'c(c, d)')
  Children(3):
      IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
        Children(1):
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
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

            Dim compilation = CreateEmptyCompilation({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
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

            Dim compilation = CreateEmptyCompilation({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
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
IEndOperation (OperationKind.End, Type: null) (Syntax: 'End')
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
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If i = 0 Th ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i = 0')
      Left: 
        IFieldReferenceOperation: C.i As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
          Instance Receiver: 
            null
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If i = 0 Th ... End If')
      IEndOperation (OperationKind.End, Type: null) (Syntax: 'End')
  WhenFalse: 
    null
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
IStopOperation (OperationKind.Stop, Type: null) (Syntax: 'Stop')
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
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If i = 0 Th ... End If')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i = 0')
      Left: 
        IFieldReferenceOperation: C.i As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
          Instance Receiver: 
            null
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  WhenTrue: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If i = 0 Th ... End If')
      IStopOperation (OperationKind.Stop, Type: null) (Syntax: 'Stop')
  WhenFalse: 
    null
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
ICatchClauseOperation (Exception type: System.Exception) (OperationKind.CatchClause, Type: null) (Syntax: 'Catch ex As ...  Is Nothing')
  Locals: Local_1: ex As System.Exception
  ExceptionDeclarationOrExpression: 
    IVariableDeclaratorOperation (Symbol: ex As System.Exception) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ex')
      Initializer: 
        null
  Filter: 
    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'ex Is Nothing')
      Left: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'ex')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: ex (OperationKind.LocalReference, Type: System.Exception) (Syntax: 'ex')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
  Handler: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Catch ex As ...  Is Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of CatchBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestSubSingleLineLambda()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub Method()
        Dim a As Action = Sub() Console.Write("hello")'BIND:"Sub() Console.Write("hello")"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub() Conso ... te("hello")')
  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... te("hello")')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write("hello")')
      Expression: 
        IInvocationOperation (Sub System.Console.Write(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write("hello")')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"hello"')
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "hello") (Syntax: '"hello"')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... te("hello")')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Sub() Conso ... te("hello")')
      ReturnedValue: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestFunctionSingleLineLambda()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub Method()
        Dim a As Func(Of Integer) = Function() 1'BIND:"Function() 1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() 1')
  IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() 1')
    Locals: Local_1: <anonymous local> As System.Int32
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
      ReturnedValue: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() 1')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() 1')
      ReturnedValue: 
        ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'Function() 1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestFunctionMultiLineLambda()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub Method()
        Dim a As Func(Of Integer) = Function()'BIND:"Function()"
                                        Return 1
                                    End Function
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Function () As System.Int32) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function()' ... nd Function')
  IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function()' ... nd Function')
    Locals: Local_1: <anonymous local> As System.Int32
    IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return 1')
      ReturnedValue: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
      ReturnedValue: 
        ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'End Function')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestSubMultiLineLambda()
            Dim source = <![CDATA[
Imports System

Class Test
    Sub Method()
        Dim a As Action = Sub()'BIND:"Sub()"
                              Return
                          End Sub
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Sub ()) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Sub()'BIND: ... End Sub')
  IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Sub()'BIND: ... End Sub')
    IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return')
      ReturnedValue: 
        null
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
      ReturnedValue: 
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(23001, "https://github.com/dotnet/roslyn/issues/23001")>
        Public Sub TestGetOperationForQualifiedName()
            Dim source = <![CDATA[
Public Class Test
	Private Class A
		Public b As B
	End Class
	Private Class B
	End Class

	Private Sub M(a As A)
		Dim x2 As Integer = a.b'BIND:"a.b"
	End Sub
End Class
]]>.Value

            Dim comp = CreateVisualBasicCompilation(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            ' Verify we return non-null operation only for topmost member access expression.
            Dim expr = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(comp, tree.FilePath)
            Assert.Equal("a.b", expr.ToString())
            Dim operation = model.GetOperation(expr)
            Assert.NotNull(operation)
            Assert.Equal(OperationKind.FieldReference, operation.Kind)
            Dim fieldOperation = DirectCast(operation, IFieldReferenceOperation)
            Assert.Equal("b", fieldOperation.Field.Name)

            ' Verify we return null operation for child nodes of member access expression.
            Assert.Null(model.GetOperation(expr.Name))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23283, "https://github.com/dotnet/roslyn/issues/23283")>
        <Fact()>
        Public Sub TestOptionStatement_01()
            Dim source = <![CDATA[
Class Test
    Sub Method()
        Option Strict On 'BIND:"Option Strict On"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Option Strict On')
  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30024: Statement is not valid inside a method.
        Option Strict On 'BIND:"Option Strict On"
        ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of StatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23283, "https://github.com/dotnet/roslyn/issues/23283")>
        <Fact()>
        Public Sub TestOptionStatement_02()
            Dim source = <![CDATA[
Option Strict On 'BIND:"Option Strict On"
]]>.Value

            VerifyNoOperationTreeForTest(Of StatementSyntax)(source)
        End Sub
    End Class
End Namespace
