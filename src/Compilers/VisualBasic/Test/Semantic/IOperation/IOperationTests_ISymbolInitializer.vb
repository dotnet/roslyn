' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits BasicTestBase

        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub NoInitializers()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
	Shared s1 As Integer
	Private i1 As Integer
End Class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of VariableDeclaratorSyntax)().ToArray()
            Assert.Equal(2, nodes.Length)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            For Each node In nodes
                Assert.Null(semanticModel.GetOperationInternal(node))
            Next
        End Sub

        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ConstantInitializers()
            Dim source = <![CDATA[
Class C
	Shared s1 As Integer = 1
	Private i1 As Integer = 1

	Private Sub M(Optional p1 As Integer = 0, Optional ParamArray p2 As Integer() = Nothing)
    End Sub
End Class
]]>.Value
            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of EqualsValueSyntax)().ToArray()
            Assert.Equal(4, nodes.Length)

            compilation.VerifyOperationTree(nodes(0), expectedOperationTree:=
            <![CDATA[IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value)

            compilation.VerifyOperationTree(nodes(1), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.i1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value)

            compilation.VerifyOperationTree(nodes(2), expectedOperationTree:=<![CDATA[
IParameterInitializer (Parameter: [p1 As System.Int32 = 0]) (OperationKind.ParameterInitializerAtDeclaration)
  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
]]>.Value)

            compilation.VerifyOperationTree(nodes(3), expectedOperationTree:=<![CDATA[
IParameterInitializer (Parameter: [ParamArray p2 As System.Int32() = Nothing]) (OperationKind.ParameterInitializerAtDeclaration)
  IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32(), Constant: null)
    ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null)
]]>.Value)
        End Sub

        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub ExpressionInitializers()
            Dim source = <![CDATA[
Class C
	Shared s1 As Integer = 1 + Foo()
	Private i1 As Integer = 1 + Foo()

	Private Shared Function Foo() As Integer
		Return 1
	End Function
End Class
]]>.Value

            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of EqualsValueSyntax)().ToArray()
            Assert.Equal(2, nodes.Length)

            compilation.VerifyOperationTree(nodes(0), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: IInvocationExpression (static Function C.Foo() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value)

            compilation.VerifyOperationTree(nodes(1), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.i1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: IInvocationExpression (static Function C.Foo() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value)
        End Sub

        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub PartialClasses()
            Dim source = <![CDATA[
Partial Class C
	Shared s1 As Integer = 1
	Private i1 As Integer = 1
End Class
Partial Class C
	Shared s2 As Integer = 2
	Private i2 As Integer = 2
End Class
]]>.Value

            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of EqualsValueSyntax)().ToArray()
            Assert.Equal(4, nodes.Length)

            compilation.VerifyOperationTree(nodes(0), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.s1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value)

            compilation.VerifyOperationTree(nodes(1), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.i1 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value)

            compilation.VerifyOperationTree(nodes(2), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.s2 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value)

            compilation.VerifyOperationTree(nodes(3), expectedOperationTree:=<![CDATA[
IFieldInitializer (Field: C.i2 As System.Int32) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value)
        End Sub

        <Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")>
        Public Sub MemberInitializer()
            Dim source = <![CDATA[
Structure Bar
	Public Field As Boolean
End Structure

Class Foo
	Public Field As Integer
	Public Property Property1() As String
	Public Property Property2() As Bar
End Class

Class C
	Public Sub M1()
		Dim x1 = New Foo()
		Dim x2 = New Foo() With { .Field = 2 }
		Dim x3 = New Foo() With { .Property1 = "" }
		Dim x4 = New Foo() With { .Property1 = "",  .Field = 2 }
		Dim x5 = New Foo() With { .Property2 = New Bar() With { .Field = True } }

		Dim e1 = New Foo() With { .Property2 = 1 }
		Dim e2 = New Foo() From { "" }
	End Sub
End Class
]]>.Value
            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll, parseOptions:=TestOptions.Regular)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).ToArray()
            Assert.Equal(7, nodes.Length)

            compilation.VerifyOperationTree(nodes(0), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: x1 As Foo (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
]]>.Value)

            compilation.VerifyOperationTree(nodes(1), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: x2 As Foo (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IFieldInitializer (Field: Foo.Field As System.Int32) (OperationKind.FieldInitializerInCreation)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value)

            compilation.VerifyOperationTree(nodes(2), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: x3 As Foo (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: Property Foo.Property1 As System.String) (OperationKind.PropertyInitializerInCreation)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
]]>.Value)

            compilation.VerifyOperationTree(nodes(3), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: x4 As Foo (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: Property Foo.Property1 As System.String) (OperationKind.PropertyInitializerInCreation)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
          IFieldInitializer (Field: Foo.Field As System.Int32) (OperationKind.FieldInitializerInCreation)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value)

            compilation.VerifyOperationTree(nodes(4), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: x5 As Foo (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: Property Foo.Property2 As Bar) (OperationKind.PropertyInitializerInCreation)
            IObjectCreationExpression (Constructor: Sub Bar..ctor()) (OperationKind.ObjectCreationExpression, Type: Bar)
              Member Initializers: IFieldInitializer (Field: Bar.Field As System.Boolean) (OperationKind.FieldInitializerInCreation)
                  ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
]]>.Value)

            compilation.VerifyOperationTree(nodes(5), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: e1 As Foo (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo, IsInvalid)
        Member Initializers: IPropertyInitializer (Property: Property Foo.Property2 As Bar) (OperationKind.PropertyInitializerInCreation, IsInvalid)
            IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Bar, IsInvalid)
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value)

            compilation.VerifyOperationTree(nodes(6), expectedOperationTree:=<![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: e2 As Foo (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IObjectCreationExpression (Constructor: Sub Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo, IsInvalid)
]]>.Value)
        End Sub
    End Class
End Namespace
