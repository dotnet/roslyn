' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_BadReceiver()
            Dim source = <![CDATA[
Imports System

Class Program
    Public Shared Sub Main(args As String())
        Console.WriteLine2()'BIND:"Console.WriteLine2()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2()')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2')
        Children(1):
            IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Console')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'WriteLine2' is not a member of 'Console'.
        Console.WriteLine2()'BIND:"Console.WriteLine2()"
        ~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_OverloadResolutionFailureBadArgument()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        F(String.Empty)'BIND:"F(String.Empty)"
    End Sub

    Private Sub F(x As Integer)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvocationOperation ( Sub Program.F(x As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F(String.Empty)')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'F')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'String.Empty')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'String.Empty')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IFieldReferenceOperation: System.String.Empty As System.String (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'String.Empty')
              Instance Receiver: 
                null
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        F(String.Empty)'BIND:"F(String.Empty)"
        ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidInvocationExpression_OverloadResolutionFailureExtraArgument()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        F(String.Empty)'BIND:"F(String.Empty)"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'F(String.Empty)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null) (Syntax: 'F')
        Children(1):
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'F')
      IFieldReferenceOperation: System.String.Empty As System.String (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'String.Empty')
        Instance Receiver: 
          null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Private Sub F()'.
        F(String.Empty)'BIND:"F(String.Empty)"
          ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidFieldReferenceExpression()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y = x.MissingField'BIND:"x.MissingField"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x.MissingField')
  Children(1):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'MissingField' is not a member of 'Program'.
        Dim y = x.MissingField'BIND:"x.MissingField"
                ~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidConversionExpression_ImplicitCast()
            Dim source = <![CDATA[
Class Program
    Private i1 As Integer
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y As Program = x.i1'BIND:"x.i1"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldReferenceOperation: Program.i1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
  Instance Receiver: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Integer' cannot be converted to 'Program'.
        Dim y As Program = x.i1'BIND:"x.i1"
                           ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidConversionExpression_ExplicitCast()
            Dim source = <![CDATA[
Class Program
    Private i1 As Integer
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Dim y As Program = DirectCast(x.i1, Program)'BIND:"DirectCast(x.i1, Program)"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsInvalid) (Syntax: 'DirectCast( ... 1, Program)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    IFieldReferenceOperation: Program.i1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
      Instance Receiver: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Integer' cannot be converted to 'Program'.
        Dim y As Program = DirectCast(x.i1, Program)'BIND:"DirectCast(x.i1, Program)"
                                      ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of DirectCastExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidUnaryExpression()
            Dim source = <![CDATA[
Imports System

Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Console.Write(+x)'BIND:"+x"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUnaryOperation (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperator, Type: ?, IsInvalid) (Syntax: '+x')
  Operand: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30487: Operator '+' is not defined for type 'Program'.
        Console.Write(+x)'BIND:"+x"
                      ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidBinaryExpression()
            Dim source = <![CDATA[
Imports System

Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Program()
        Console.Write(x + (y * args.Length))'BIND:"x + (y * args.Length)"
    End Sub

    Private Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'x + (y * args.Length)')
  Left: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program) (Syntax: 'x')
  Right: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: ?, IsInvalid) (Syntax: '(y * args.Length)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'y * args.Length')
          Left: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'y')
              Children(0)
          Right: 
            IPropertyReferenceOperation: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
              Instance Receiver: 
                IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
        Console.Write(x + (y * args.Length))'BIND:"x + (y * args.Length)"
                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of BinaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidLambdaBinding_UnboundLambda()
            Dim source = <![CDATA[
Imports System

Class Program
    Public Shared Sub Main(args As String())
        Dim x = Function() F()'BIND:"Function() F()"
    End Sub

    Private Shared Sub F()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousFunctionOperation (Symbol: Function () As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() F()')
  IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() F()')
    Locals: Local_1: <anonymous local> As ?
    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
      ReturnedValue: 
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'F()')
          Children(1):
              IInvocationOperation (Sub Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                Instance Receiver: 
                  null
                Arguments(0)
    ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() F()')
      Statement: 
        null
    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() F()')
      ReturnedValue: 
        ILocalReferenceOperation:  (OperationKind.LocalReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Function() F()')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Dim x = Function() F()'BIND:"Function() F()"
                           ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of SingleLineLambdaExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidFieldInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Class Program
    Private x As Integer = Program'BIND:"= Program"
    Public Shared Sub Main(args As String())
        Dim x = New Program() With {
            .x = Program
        }
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: Program.x As System.Int32) (OperationKind.FieldInitializer, Type: null, IsInvalid) (Syntax: '= Program')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Program')
    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30109: 'Program' is a class type and cannot be used as an expression.
    Private x As Integer = Program'BIND:"= Program"
                           ~~~~~~~
BC30109: 'Program' is a class type and cannot be used as an expression.
            .x = Program
                 ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18074"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidArrayInitializer()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New Integer(1, 1) {{{1, 1}}, {2, 2}}'BIND:"{{1, 1}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{1, 1}}')
  Element Values(1): IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{1, 1}')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30567: Array initializer is missing 1 elements.
        Dim x = New Integer(1, 1) {{{1, 1}}, {2, 2}}'BIND:"{{1, 1}}"
                                   ~~~~~~~~
BC30566: Array initializer has too many dimensions.
        Dim x = New Integer(1, 1) {{{1, 1}}, {2, 2}}'BIND:"{{1, 1}}"
                                    ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidArrayCreation()
            Dim source = <![CDATA[
Class Program
    Public Shared Sub Main(args As String())
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IArrayCreationOperation (OperationKind.ArrayCreation, Type: X(), IsInvalid) (Syntax: 'New X(Program - 1) {{1}}')
  Dimension Sizes(1):
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Program - 1')
        Left: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Program - 1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'Program - 1')
                Left: 
                  IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Program')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'Program - 1')
  Initializer: 
    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{{1}}')
      Element Values(1):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '{1}')
            Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'X' is not defined.
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
                    ~
BC30109: 'Program' is a class type and cannot be used as an expression.
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
                      ~~~~~~~
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
                                   ~~~~~
BC30566: Array initializer has too many dimensions.
        Dim x = New X(Program - 1) {{1}}'BIND:"New X(Program - 1) {{1}}"
                                    ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ArrayCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")>
        Public Sub InvalidParameterDefaultValueInitializer()
            Dim source = <![CDATA[
Class Program
    Private Shared Function M() As Integer
        Return 0
    End Function
    Private Sub F(Optional p As Integer = M())'BIND:"= M()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterInitializerOperation (Parameter: [p As System.Int32]) (OperationKind.ParameterInitializer, Type: null, IsInvalid) (Syntax: '= M()')
  IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
    Children(1):
        IInvocationOperation (Function Program.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M()')
          Instance Receiver: 
            null
          Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Private Sub F(Optional p As Integer = M())'BIND:"= M()"
                                          ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInvalid_InstanceIndexerAccessOnClass()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module M1
    Class C1
        Default Public ReadOnly Property Item(i As Integer) As C1
            'indexer
            Get
                Return Nothing
            End Get
        End Property
    End Class

    Sub S1()
        Dim a = C1(1)'BIND:"C1(1)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'C1(1)')
  Children(2):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'C1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30109: 'M1.C1' is a class type and cannot be used as an expression.
        Dim a = C1(1)'BIND:"C1(1)"
                ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
