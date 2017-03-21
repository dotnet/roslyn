' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
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
Imports System
Imports System.Collections.Generic
Imports System.Linq

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
        Public Sub MixedDimAsNewAndEqualsDeclarations()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

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



        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/17913"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub UsingStatementDeclarationAsNew()
            Dim source = <![CDATA[
Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As New C'BIND:"Dim c1 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement)
  IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: c1 As Program.C (OperationKind.VariableDeclaration)
      Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/17913"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
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
IUsingStatement (OperationKind.UsingStatement)
  IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: c1 As Program.C (OperationKind.VariableDeclaration)
      Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

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
    End Class
End Namespace
