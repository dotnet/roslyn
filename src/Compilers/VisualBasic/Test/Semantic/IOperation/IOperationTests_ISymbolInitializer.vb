' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
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

            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().Where(Function(n) TryCast(n, VariableDeclaratorSyntax) IsNot Nothing OrElse TryCast(n, PropertyStatementSyntax) IsNot Nothing).ToArray()
            Assert.Equal(3, nodes.Length)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            For Each node In nodes
                Assert.Null(semanticModel.GetOperationInternal(node))
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
IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IFieldInitializer (Field: C.i2 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 2')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IPropertyInitializer (Property: Property C.P1 As System.Int32) (OperationKind.PropertyInitializer) (Syntax: '= 1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IParameterInitializer (Parameter: [p1 As System.Int32 = 0]) (OperationKind.ParameterInitializer) (Syntax: '= 0')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IParameterInitializer (Parameter: [ParamArray p2 As System.Int32() = Nothing]) (OperationKind.ParameterInitializer) (Syntax: '= Nothing')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'Nothing')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
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
IFieldInitializer (Field: C.x As System.Object) (OperationKind.FieldInitializer) (Syntax: 'As New Object')
  IObjectCreationExpression (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'New Object')
    Arguments(0)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/17813"), WorkItem(17813, "https://github.com/dotnet/roslyn/issues/17813")>
        Public Sub MultipleFieldInitializers()
            Dim source = <![CDATA[
Class C
    Dim x, y As New Object'BIND:"As New Object"
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IFieldInitializer (2 initialized fields) (OperationKind.FieldInitializer)  (Syntax: 'As New Object')
  Field_1: C.x As System.Object
  Field_2: C.y As System.Object
  IObjectCreationExpression (Constructor: Sub System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'New Object')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationExpression (Function C.F() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
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
IFieldInitializer (Field: C.i1 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationExpression (Function C.F() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
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
IPropertyInitializer (Property: Property C.P1 As System.Int32) (OperationKind.PropertyInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: 
      IInvocationExpression (Function C.F() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
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
IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IFieldInitializer (Field: C.i2 As System.Int32) (OperationKind.FieldInitializer) (Syntax: '= 2')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IFieldInitializer (Field: C.s1 As System.Byte) (OperationKind.FieldInitializer, IsInvalid) (Syntax: '= 0.0')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
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
IFieldInitializer (Field: C.s1 As System.Byte) (OperationKind.FieldInitializer) (Syntax: '= 0')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 0, IsImplicit) (Syntax: '0')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
