' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub NoInitializers()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
	Shared s1 As Integer
	Private i1 As Integer
    Private Property P1 As Integer
End Class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().Where(Function(n) TryCast(n, VariableDeclaratorSyntax) IsNot Nothing OrElse TryCast(n, PropertyStatementSyntax) IsNot Nothing).ToArray()
            Assert.Equal(3, nodes.Length)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            For Each node In nodes
                Assert.Null(semanticModel.GetOperation(node))
            Next
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers_StaticField()
            Dim source = <![CDATA[
Class C
    Shared s1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers_InstanceField()
            Dim source = <![CDATA[
Class C
    Private i1 As Integer = 1, i2 As Integer = 2'BIND:"= 2"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.i2 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 2')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers_Property()
            Dim source = <![CDATA[
Class C
    Private Property P1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyInitializerOperation (Property: Property C.P1 As System.Int32) (OperationKind.PropertyInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers_DefaultValueParameter()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= 0"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterInitializerOperation (Parameter: [p1 As System.Int32 = 0]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 0')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30642: 'Optional' and 'ParamArray' cannot be combined.
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= 0"
                                                       ~~~~~~~~~~
BC30046: Method cannot have both a ParamArray and Optional parameters.
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= 0"
                                                                  ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers_DefaultValueParamsArray()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= Nothing"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IParameterInitializerOperation (Parameter: [ParamArray p2 As System.Int32() = Nothing]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= Nothing')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'Nothing')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30642: 'Optional' and 'ParamArray' cannot be combined.
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= Nothing"
                                                       ~~~~~~~~~~
BC30046: Method cannot have both a ParamArray and Optional parameters.
    Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)'BIND:"= Nothing"
                                                                  ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub AsNewFieldInitializer()
            Dim source = <![CDATA[
Class C
    Dim x As New Object'BIND:"As New Object"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.x As System.Object) (OperationKind.FieldInitializer, Type: null) (Syntax: 'As New Object')
  IObjectCreationOperation (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'New Object')
    Arguments(0)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub MultipleFieldInitializers()
            Dim source = <![CDATA[
Class C
    Dim x, y As New Object'BIND:"As New Object"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (2 initialized fields) (OperationKind.FieldInitializer, Type: null) (Syntax: 'As New Object')
  Field_1: C.x As System.Object
  Field_2: C.y As System.Object
  IObjectCreationOperation (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'New Object')
    Arguments(0)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub SingleFieldInitializerErrorCase()
            Dim source = <![CDATA[
Class C1
    Dim x, y As Object = Me'BIND:"= Me"
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (2 initialized fields) (OperationKind.FieldInitializer, Type: null, IsInvalid) (Syntax: '= Me')
  Field_1: C1.x As System.Object
  Field_2: C1.y As System.Object
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'Me')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid) (Syntax: 'Me')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    Dim x, y As Object = Me'BIND:"= Me"
        ~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub MultipleWithEventsInitializers()
            Dim source = <![CDATA[
Class C1
    Public Sub New(c As C1)
    End Sub

    WithEvents e, f As New C1(Me)'BIND:"As New C1(Me)"
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyInitializerOperation (2 initialized properties) (OperationKind.PropertyInitializer, Type: null) (Syntax: 'As New C1(Me)')
  Property_1: WithEvents C1.e As C1
  Property_2: WithEvents C1.f As C1
  IObjectCreationOperation (Constructor: Sub C1..ctor(c As C1)) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1(Me)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'Me')
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'Me')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub SingleWithEventsInitializersErrorCase()
            Dim source = <![CDATA[
Class C1
    Public Sub New(c As C1)
    End Sub
    WithEvents e, f As C1 = Me'BIND:"= Me"
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyInitializerOperation (2 initialized properties) (OperationKind.PropertyInitializer, Type: null, IsInvalid) (Syntax: '= Me')
  Property_1: WithEvents C1.e As C1
  Property_2: WithEvents C1.f As C1
  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1, IsInvalid) (Syntax: 'Me')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    WithEvents e, f As C1 = Me'BIND:"= Me"
               ~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ExpressionInitializers_StaticField()
            Dim source = <![CDATA[
Class C
    Shared s1 As Integer = 1 + F()'BIND:"= 1 + F()"

    Private Shared Function F() As Integer
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (Function C.F() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ExpressionInitializers_InstanceField()
            Dim source = <![CDATA[
Class C
    Private i1 As Integer = 1 + F()'BIND:"= 1 + F()"

    Private Shared Function F() As Integer
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.i1 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (Function C.F() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ExpressionInitializers_Property()
            Dim source = <![CDATA[
Class C
    Private Property P1 As Integer = 1 + F()'BIND:"= 1 + F()"

    Private Shared Function F() As Integer
        Return 1
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyInitializerOperation (Property: Property C.P1 As System.Int32) (OperationKind.PropertyInitializer, Type: null) (Syntax: '= 1 + F()')
  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationOperation (Function C.F() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: 
          null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub PartialClasses_StaticField()
            Dim source = <![CDATA[
Partial Class C
    Shared s1 As Integer = 1'BIND:"= 1"
    Private i1 As Integer = 1
End Class
Partial Class C
    Shared s2 As Integer = 2
    Private i2 As Integer = 2
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub PartialClasses_InstanceField()
            Dim source = <![CDATA[
Partial Class C
    Shared s1 As Integer = 1
    Private i1 As Integer = 1
End Class
Partial Class C
    Shared s2 As Integer = 2
    Private i2 As Integer = 2'BIND:"= 2"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.i2 As System.Int32) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 2')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")>
        Public Sub FieldInitializer_ConstantConversions_01()
            Dim source = <![CDATA[
Option Strict On
Class C
    Private s1 As Byte = 0.0'BIND:"= 0.0"
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Byte) (OperationKind.FieldInitializer, Type: null, IsInvalid) (Syntax: '= 0.0')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Byte'.
    Private s1 As Byte = 0.0'BIND:"= 0.0"
                         ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(7299, "https://github.com/dotnet/roslyn/issues/7299")>
        Public Sub FieldInitializer_ConstantConversions_02()
            Dim source = <![CDATA[
Option Strict On
Class C
    Private s1 As Byte = 0'BIND:"= 0"
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializerOperation (Field: C.s1 As System.Byte) (OperationKind.FieldInitializer, Type: null) (Syntax: '= 0')
  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, Constant: 0, IsImplicit) (Syntax: '0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ConstantInitializer_NonConstantField()
            Dim source = <![CDATA[
Class C
    Shared s1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ConstantInitializer_ConstantField()
            Dim source = <![CDATA[
Class C
    Const s1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantInitializer_NonConstantStaticField()
            ' This unit test also includes declaration with multiple variables.
            Dim source = <![CDATA[
Class C
    Shared s0 As Integer = 0, s1 As Integer = M(), s2 As Integer 'BIND:"= M()"
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantInitializer_NonConstantInstanceField()
            Dim source = <![CDATA[
Class C
    Private s1 As Integer = M()'BIND:"= M()"
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M()')
          Right: 
            IInvocationOperation (Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantInitializer_ConstantField()
            Dim source = <![CDATA[
Class C
    Const s1 As Integer = M()'BIND:"= M()"
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M()')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Const s1 As Integer = M()'BIND:"= M()"
                          ~~~

]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_AsNewFieldInitializer()
            Dim source = <![CDATA[
Class C
    Private s1, s2 As New Integer()'BIND:"As New Integer()"
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New Integer()')
          Right: 
            IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer()')
              Arguments(0)
              Initializer: 
                null

        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
          Left: 
            IFieldReferenceOperation: C.s2 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New Integer()')
          Right: 
            IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer()')
              Arguments(0)
              Initializer: 
                null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ArrayDeclaration()
            Dim source = <![CDATA[
Class C
    Private s1(10) As Integer = Nothing'BIND:"= Nothing"
End Class
]]>.Value

            ' Expected Flow graph should also include the implicit array initializer (10).
            ' This is tracked by https://github.com/dotnet/roslyn/issues/26780
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: '= Nothing')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32() (OperationKind.FieldReference, Type: System.Int32(), IsImplicit) (Syntax: '= Nothing')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= Nothing')
          Right: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'Nothing')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNothingLiteral)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
                BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
    Private s1(10) As Integer = Nothing'BIND:"= Nothing"
            ~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_ConstantInitializer_ConstantField()
            Dim source = <![CDATA[
Class C
    Const s1 As Integer = If(True, 1, 2)'BIND:"= If(True, 1, 2)"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block [UnReachable]
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= If(True, 1, 2)')
              Left: 
                IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= If(True, 1, 2)')
                  Instance Receiver: 
                    null
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'If(True, 1, 2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantInitializer_NonConstantStaticField()
            Dim source = <![CDATA[
Class C
    Private Shared s1 As Integer = If(M(), M2)'BIND:"= If(M(), M2)"
    Public Shared Function M() As Integer?
        Return 0
    End Function
    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation (Function C.M() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32)) (Syntax: 'M()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2)')
              Left: 
                IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2)')
                  Instance Receiver: 
                    null
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(M(), M2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantInitializer_NonConstantInstanceField()
            Dim source = <![CDATA[
Class C
    Private s1 As Integer = If(M(), M2)'BIND:"= If(M(), M2)"
    Public Shared Function M() As Integer?
        Return 0
    End Function
    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation (Function C.M() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32)) (Syntax: 'M()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2)')
              Left: 
                IFieldReferenceOperation: C.s1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= If(M(), M2)')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(M(), M2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantInitializer_ConstantField()
            Dim source = <![CDATA[
Class C
    Const s1 As Integer = If(M(), M2)'BIND:"= If(M(), M2)"
    Public Shared Function M() As Integer?
        Return 0
    End Function
    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation (Function C.M() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'M()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'M()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'M()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'M()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= If(M(), M2)')
              Left: 
                IFieldReferenceOperation: C.s1 As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= If(M(), M2)')
                  Instance Receiver: 
                    null
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(M(), M2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Const s1 As Integer = If(M(), M2)'BIND:"= If(M(), M2)"
                          ~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_AsNewFieldInitializer()
            Dim source = <![CDATA[
Class C
    Private s1, s2 As New C(If(a, b))'BIND:"As New C(If(a, b))"
    Private Shared a As Integer? = 1
    Private Shared b As Integer = 1
    Public Sub New(a As Integer)
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFieldReferenceOperation: C.a As System.Nullable(Of System.Int32) (Static) (OperationKind.FieldReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IFieldReferenceOperation: C.b As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Left: 
                IFieldReferenceOperation: C.s1 As C (OperationKind.FieldReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(If(a, b))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'If(a, b)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
            Entering: {R3} {R4}
}
.locals {R3}
{
    CaptureIds: [3]
    .locals {R4}
    {
        CaptureIds: [2]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFieldReferenceOperation: C.a As System.Nullable(Of System.Int32) (Static) (OperationKind.FieldReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                Leaving: {R4}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IFieldReferenceOperation: C.b As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Left: 
                IFieldReferenceOperation: C.s2 As C (OperationKind.FieldReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(If(a, b))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'If(a, b)')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B9]
            Leaving: {R3}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_MissingFieldInitializerValue()
            Dim source = <![CDATA[
Class C
    Public s1 As Integer = 'BIND:"="
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= ')
          Left: 
            IFieldReferenceOperation: C.s1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= ')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: '= ')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
    Public s1 As Integer = 'BIND:"="
                           ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ConstantPropertyInitializer_StaticProperty()
            Dim source = <![CDATA[
Class C
    Shared Property P1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IPropertyReferenceOperation: Property C.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ConstantPropertyInitializer_InstanceProperty()
            Dim source = <![CDATA[
Class C
    Public Property P1 As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= 1')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantPropertyInitializer_StaticProperty()
            Dim source = <![CDATA[
Class C
    Shared Property P1 As Integer = M()'BIND:"= M()"
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IPropertyReferenceOperation: Property C.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                null
          Right: 
            IInvocationOperation (Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantPropertyInitializer_InstanceProperty()
            Dim source = <![CDATA[
Class C
    Public Property P1 As Integer = M()'BIND:"= M()"
    Public Shared Function M() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= M()')
          Left: 
            IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= M()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= M()')
          Right: 
            IInvocationOperation (Function C.M() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
              Instance Receiver: 
                null
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_PropertyInitializerWithArguments()
            Dim source = <![CDATA[
Class C
    Shared Property P1(i As Integer) As Integer = 1'BIND:"= 1"
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IPropertyReferenceOperation: Property C.P1(i As System.Int32) As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '= 1')
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: '= 1')
                      Children(0)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36759: Auto-implemented properties cannot have parameters.
    Shared Property P1(i As Integer) As Integer = 1'BIND:"= 1"
                      ~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_AsNewPropertyInitializer()
            Dim source = <![CDATA[
Class C
    Private ReadOnly Property P1 As New Integer()'BIND:"As New Integer()"
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
          Left: 
            IPropertyReferenceOperation: ReadOnly Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'As New Integer()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New Integer()')
          Right: 
            IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer()')
              Arguments(0)
              Initializer: 
                null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_WithEventsAsNewPropertyInitializer()
            Dim source = <![CDATA[
Class C
    WithEvents e, f As New C() 'BIND:"As New C()"
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C()')
          Left: 
            IPropertyReferenceOperation: WithEvents C.e As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'As New C()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C()')
          Right: 
            IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C()')
              Arguments(0)
              Initializer: 
                null

        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C()')
          Left: 
            IPropertyReferenceOperation: WithEvents C.f As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'As New C()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C()')
          Right: 
            IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C()')
              Arguments(0)
              Initializer: 
                null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantPropertyInitializer_StaticProperty()
            Dim source = <![CDATA[
Class C
    Shared Property P1 As Integer = If(M(), M2())'BIND:"= If(M(), M2())"
    Public Shared Function M() As Integer?
        Return 0
    End Function
    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation (Function C.M() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32)) (Syntax: 'M()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2())')
              Left: 
                IPropertyReferenceOperation: Property C.P1 As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2())')
                  Instance Receiver: 
                    null
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(M(), M2())')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantPropertyInitializer_InstanceProperty()
            Dim source = <![CDATA[
Class C
    Public Property P1 As Integer = If(M(), M2())'BIND:"= If(M(), M2())"
    Public Shared Function M() As Integer?
        Return 0
    End Function
    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation (Function C.M() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32)) (Syntax: 'M()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'M()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'M()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'M()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2())')
              Left: 
                IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '= If(M(), M2())')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: '= If(M(), M2())')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(M(), M2())')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_AsNewPropertyInitializer()
            Dim source = <![CDATA[
Class C
    Private ReadOnly Property P1 As New C(If(a, b))'BIND:"As New C(If(a, b))"
    Private Shared a As Integer? = 1
    Private Shared b As Integer = 1
    Public Sub New(a As Integer)
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFieldReferenceOperation: C.a As System.Nullable(Of System.Int32) (Static) (OperationKind.FieldReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IFieldReferenceOperation: C.b As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Left: 
                IPropertyReferenceOperation: ReadOnly Property C.P1 As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(If(a, b))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'If(a, b)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_WithEventsAsNewPropertyInitializer()
            Dim source = <![CDATA[
Class C
    WithEvents e, f As New C(If(a, b)) 'BIND:"As New C(If(a, b))"
    Private Shared a As Integer? = 1
    Private Shared b As Integer = 1
    Public Sub New(a As Integer)
    End Sub
End Class
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFieldReferenceOperation: C.a As System.Nullable(Of System.Int32) (Static) (OperationKind.FieldReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IFieldReferenceOperation: C.b As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Left: 
                IPropertyReferenceOperation: WithEvents C.e As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(If(a, b))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'If(a, b)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B5]
            Leaving: {R1}
            Entering: {R3} {R4}
}
.locals {R3}
{
    CaptureIds: [3]
    .locals {R4}
    {
        CaptureIds: [2]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFieldReferenceOperation: C.a As System.Nullable(Of System.Int32) (Static) (OperationKind.FieldReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'a')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                Leaving: {R4}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'a')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IFieldReferenceOperation: C.b As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b')
                  Instance Receiver: 
                    null

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Left: 
                IPropertyReferenceOperation: WithEvents C.f As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'As New C(If(a, b))')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(If(a, b))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'If(a, b)')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(a, b)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

        Next (Regular) Block[B9]
            Leaving: {R3}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_MissingPropertyInitializerValue()
            Dim source = <![CDATA[
Class C
    Public Property P1 As Integer ='BIND:"="
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '=')
          Left: 
            IPropertyReferenceOperation: Property C.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '=')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: '=')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
    Public Property P1 As Integer ='BIND:"="
                                   ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_ConstantParameterInitializer()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer = 1)'BIND:"= 1"
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Left: 
            IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: '= 1')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_NonConstantParameterInitializer()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer = M1())'BIND:"= M1()"
    End Sub
    Private Shared Function M1() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1()')
          Left: 
            IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= M1()')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1()')
              Children(1):
                  IInvocationOperation (Function C.M1() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M1()')
                    Instance Receiver: 
                      null
                    Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Private Sub M(Optional p1 As Integer = M1())'BIND:"= M1()"
                                           ~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ControlFlow_NonConstantParameterInitializer()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer = If(M1(), M2()))'BIND:"= If(M1(), M2())"
    End Sub
    Private Shared Function M1() As Integer?
        Return 0
    End Function
    Private Shared Function M2() As Integer
        Return 0
    End Function
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1()')
                  Value: 
                    IInvocationOperation (Function C.M1() As System.Nullable(Of System.Int32)) (OperationKind.Invocation, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 'M1()')
                      Instance Receiver: 
                        null
                      Arguments(0)

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'M1()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'M1()')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M1()')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M1()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'M1()')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2()')
              Value: 
                IInvocationOperation (Function C.M2() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= If(M1(), M2())')
              Left: 
                IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '= If(M1(), M2())')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(M1(), M2())')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'If(M1(), M2())')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
    Private Sub M(Optional p1 As Integer = If(M1(), M2()))'BIND:"= If(M1(), M2())"
                                           ~~~~~~~~~~~~~~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub NoControlFlow_MissingParameterInitializerValue()
            Dim source = <![CDATA[
Class C
    Private Sub M(Optional p1 As Integer =)'BIND:"="
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '=')
          Left: 
            IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '=')
          Right: 
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
    Private Sub M(Optional p1 As Integer =)'BIND:"="
                                          ~
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
