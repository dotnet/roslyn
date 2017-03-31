' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Semantics
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
#Region "Dim Declarations"

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As Integer'BIND:"Dim i1 As Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As Integer, i2 As Integer, b1 As Boolean'BIND:"Dim i1 As Integer, i2 As Integer, b1 As Boolean"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (3 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
  IVariableDeclaration: b1 As System.Boolean (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclarationNoType()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i1'BIND:"Dim i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub


        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarationNoTypes()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i1, i2'BIND:"Dim i1, i2"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub InvalidMultipleVariableDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i1 As Integer,'BIND:"Dim i1 As Integer,"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
  IVariableDeclaration:  As System.Object (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub InvalidMultipleVariableDeclarationsNoType()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i1,'BIND:"Dim i1,"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
  IVariableDeclaration:  As System.Object (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = 1
        Dim i2 = i1'BIND:"Dim i2 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = 1
        Dim i2 = i1, i3 = i1'BIND:"Dim i2 = i1, i3 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IVariableDeclaration: i3 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclarationExpressionInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = ReturnInt()'BIND:"Dim i1 = ReturnInt()"
    End Sub

    Function ReturnInt() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarationExpressionInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = ReturnInt(), i2 = ReturnInt()'BIND:"Dim i1 = ReturnInt(), i2 = ReturnInt()"
    End Sub

    Function ReturnInt() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub DimAsNew()
            Dim source = <![CDATA[
Module Program
    Class C
    End Class
    Sub Main(args As String())
        Dim p1 As New C'BIND:"Dim p1 As New C"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: p1 As Program.C (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleDimAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New Integer'BIND:"Dim i1, i2 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub


        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub DimAsNewNoObject()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As New'BIND:"Dim i1 As New"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleDimAsNewNoObject()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New'BIND:"Dim i1, i2 As New"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: i2 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MixedDimAsNewAndEqualsDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New Integer, b1 As Boolean = False'BIND:"Dim i1, i2 As New Integer, b1 As Boolean = False"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (3 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: b1 As System.Boolean (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MixedDimAsNewAndEqualsDeclarationsReversedOrder()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim b1 As Boolean, i1, i2 As New Integer'BIND:"Dim b1 As Boolean, i1, i2 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (3 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: b1 As System.Boolean (OperationKind.VariableDeclaration)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub DimAsNewMultipleDeclarationsSameInitializerInstance()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New Integer'BIND:"Dim i1, i2 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim compilation = CreateCompilationWithMscorlib(syntaxTree)

            Dim node As SyntaxNode = CompilationUtils.FindBindingText(Of LocalDeclarationStatementSyntax)(compilation, fileName)
            Assert.NotNull(node)

            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim operation = CType(semanticModel.GetOperationInternal(node), IVariableDeclarationStatement)

            Assert.Equal(2, operation.Variables.Count())
            Dim var1 = operation.Variables(0)
            Dim var2 = operation.Variables(1)

            Assert.NotNull(var1.InitialValue)
            Assert.NotNull(var2.InitialValue)

            Assert.Same(var1.InitialValue, var2.InitialValue)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationWithLength()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(2) As Integer'BIND:"Dim i1(2) As Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32() (OperationKind.VariableDeclaration)
    Initializer: IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32())
        IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3)
          Left: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationMultipleVariables()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(), i2 As Integer'BIND:"Dim i1(), i2 As Integer"

    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32() (OperationKind.VariableDeclaration)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationInvalidAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(2) As New Integer'BIND:"Dim i1(2) As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Int32() (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32(), IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

#End Region

#Region "Using Statements"

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/17917"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub UsingStatementDeclarationAsNew()
            Dim source = <![CDATA[
Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As New C'BIND:"c1 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: c1 As Program.C (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub


        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/17917"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub UsingStatementDeclaration()
            Dim source = <![CDATA[
Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As C = New C'BIND:"c1 As C = New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: c1 As Program.C (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

#End Region

#Region "Const Declarations"

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As Integer = 1'BIND:"Const i1 As Integer = 1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Const i1 = 1, i2 = 2'BIND:"Const i1 = 1, i2 = 2"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As New Integer'BIND:"Const i1 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstAsNewMultipleDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstSingleDeclarationNoType()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1'BIND:"Const i1 = 1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclarationsNoTypes()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1, i2 = 2'BIND:"Const i1 = 1, i2 = 2"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstSingleDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1
        Const i2 = i1'BIND:"Const i2 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclarationsLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1
        Const i2 = i1, i3 = i1'BIND:"Const i2 = i1, i3 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: i3 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstSingleDeclarationExpressionInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = Int1()'BIND:"Const i1 = Int1()"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclarationsExpressionInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = Int1(), i2 = Int1()'BIND:"Const i1 = Int1(), i2 = Int1()"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDimAsNewNoInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As New'BIND:"Const i1 As New"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDimAsNewMultipleDeclarationsNoInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: i2 As ? (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstInvalidMultipleDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1,'BIND:"Const i1 = 1,"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration:  As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

#End Region

#Region "Static Declarations"

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static i1 As Integer'BIND:"Static i1 As Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarations()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static i1, i2 As Integer'BIND:"Static i1, i2 As Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticAsNewDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static i1 As New Integer'BIND:"Static i1 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMulipleDeclarationAsNew()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static i1, i2 As New Integer'BIND:"Static i1, i2 As New Integer"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMixedAsNewAndEqualsDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Static i1, i2 As New Integer, b1 As Boolean = False'BIND:"Static i1, i2 As New Integer, b1 As Boolean = False"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (3 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Int32 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32)
  IVariableDeclaration: b1 As System.Boolean (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: False) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationNoType()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1'BIND:"Static i1 = 1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsNoTypes()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1, i2 = 2'BIND:"Static i1 = 1, i2 = 2"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1
        Static i2 = i1'BIND:"Static i2 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsLocalReferenceInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1
        Static i2 = i1, i3 = i1'BIND:"Static i2 = i1, i3 = i1"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object)
  IVariableDeclaration: i3 As System.Object (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationExpressionInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = Int1()'BIND:"Static i1 = Int1()"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        IInvocationExpression (static Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsExpressionInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = Int1(), i2 = Int1()'BIND:"Static i1 = Int1(), i2 = Int1()"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        IInvocationExpression (static Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
        IInvocationExpression (static Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticAsNewSingleDeclarationInvalidInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 As'BIND:"Static i1 As"
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticAsNewMultipleDeclarationInvalidInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1, i2 As'BIND:"Static i1, i2 As"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As ? (OperationKind.VariableDeclaration)
  IVariableDeclaration: i2 As ? (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationInvalidInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 ='BIND:"Static i1 ="
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsInvalidInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 =, i2 ='BIND:"Static i1 =, i2 ="
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: i2 As System.Object (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub



        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticInvalidMultipleDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1,'BIND:"Static i1,"
    End Sub

    Function Int1() As Integer
        Return 1
    End Function
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: i1 As System.Object (OperationKind.VariableDeclaration)
  IVariableDeclaration:  As System.Object (OperationKind.VariableDeclaration)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

#End Region
    End Class
End Namespace
