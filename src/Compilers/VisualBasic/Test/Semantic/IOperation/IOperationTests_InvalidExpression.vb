' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2()')
  Children(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Console.WriteLine2')
        Children(1):
            IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Console')
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
IInvocationExpression ( Sub Program.F(x As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F(String.Empty)')
  Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program, IsInvalid) (Syntax: 'F')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'String.Empty')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'String.Empty')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'String.Empty')
              Instance Receiver: null
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
IInvalidExpression (OperationKind.InvalidExpression, Type: System.Void, IsInvalid) (Syntax: 'F(String.Empty)')
  Children(2):
      IOperation:  (OperationKind.None) (Syntax: 'F')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'F')
      IFieldReferenceExpression: System.String.Empty As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'String.Empty')
        Instance Receiver: null
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x.MissingField')
  Children(1):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
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
IFieldReferenceExpression: Program.i1 As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
  Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
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
IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Program, IsInvalid) (Syntax: 'DirectCast( ... 1, Program)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IFieldReferenceExpression: Program.i1 As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x.i1')
      Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
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
IUnaryOperatorExpression (UnaryOperatorKind.Plus, Checked) (OperationKind.UnaryOperatorExpression, Type: ?, IsInvalid) (Syntax: '+x')
  Operand: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program, IsInvalid) (Syntax: 'x')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'x + (y * args.Length)')
  Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Right: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: ?, IsInvalid) (Syntax: '(y * args.Length)')
      Operand: IBinaryOperatorExpression (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'y * args.Length')
          Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'y')
              Children(0)
          Right: IPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'args.Length')
              Instance Receiver: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String()) (Syntax: 'args')
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
IAnonymousFunctionExpression (Symbol: Function () As ?) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function() F()')
  IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function() F()')
    Locals: Local_1: <anonymous local> As ?
    IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'F()')
      ReturnedValue: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'F()')
          Children(1):
              IInvocationExpression (Sub Program.F()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F()')
                Instance Receiver: null
                Arguments(0)
    ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsInvalid) (Syntax: 'Function() F()')
      Statement: null
    IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Function() F()')
      ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: ?, IsInvalid) (Syntax: 'Function() F()')
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
IFieldInitializer (Field: Program.x As System.Int32) (OperationKind.FieldInitializer, IsInvalid) (Syntax: '= Program')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program')
    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
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
IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: X(), IsInvalid) (Syntax: 'New X(Program - 1) {{1}}')
  Dimension Sizes(1):
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program - 1')
        Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'Program - 1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid) (Syntax: 'Program - 1')
                Left: IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Program')
                Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'Program - 1')
  Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{1}}')
      Element Values(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '{1}')
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
IParameterInitializer (Parameter: [p As System.Int32]) (OperationKind.ParameterInitializer, IsInvalid) (Syntax: '= M()')
  IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'M()')
    Children(1):
        IInvocationExpression (Function Program.M() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'M()')
          Instance Receiver: null
          Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Private Sub F(Optional p As Integer = M())'BIND:"= M()"
                                          ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
