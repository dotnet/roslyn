' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
#Region "Dim Declarations"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As Integer'BIND:"Dim i1 As Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1 As Integer'BIND:"Dim i1 As Integer"
            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As Integer, i2 As Integer, b1 As Boolean'BIND:"Dim i1 As Integer, i2 As Integer, b1 As Boolean"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (3 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I ...  As Boolean')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1 As Integer, i2 As Integer, b1 As Boolean'BIND:"Dim i1 As Integer, i2 As Integer, b1 As Boolean"
            ~~
BC42024: Unused local variable: 'i2'.
        Dim i1 As Integer, i2 As Integer, b1 As Boolean'BIND:"Dim i1 As Integer, i2 As Integer, b1 As Boolean"
                           ~~
BC42024: Unused local variable: 'b1'.
        Dim i1 As Integer, i2 As Integer, b1 As Boolean'BIND:"Dim i1 As Integer, i2 As Integer, b1 As Boolean"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1'BIND:"Dim i1"
            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1, i2'BIND:"Dim i1, i2"
            ~~
BC42024: Unused local variable: 'i2'.
        Dim i1, i2'BIND:"Dim i1, i2"
                ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As Integer,')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1 As Integer,'BIND:"Dim i1 As Integer,"
            ~~
BC30203: Identifier expected.
        Dim i1 As Integer,'BIND:"Dim i1 As Integer,"
                          ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1,')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1,'BIND:"Dim i1,"
            ~~
BC30203: Identifier expected.
        Dim i1,'BIND:"Dim i1,"
               ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub SingleVariableDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = 1
        Dim i2 = i1'BIND:"Dim i2 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i2 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleVariableDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 = 1
        Dim i2 = i1, i3 = i1'BIND:"Dim i2 = i1, i3 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i2 = i1, i3 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 = ReturnInt()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
        Instance Receiver: null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 = Re ... ReturnInt()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
        Instance Receiver: null
        Arguments(0)
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
        Instance Receiver: null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub DimAsNew()
            Dim source = <![CDATA[
Module Program
    Class C
    End Class
    Sub Main(args As String())
        Dim p1 As New C'BIND:"Dim p1 As New C"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim p1 As New C')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'p1')
    Variables: Local_1: p1 As Program.C
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C) (Syntax: 'New C')
        Arguments(0)
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleDimAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New Integer'BIND:"Dim i1, i2 As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2  ... New Integer')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub DimAsNewNoObject()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1 As New'BIND:"Dim i1 As New"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As New')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'New')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        Dim i1 As New'BIND:"Dim i1 As New"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MultipleDimAsNewNoObject()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New'BIND:"Dim i1, i2 As New"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1, i2 As New')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New')
    Variables: Local_1: i1 As ?
      Local_2: i2 As ?
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'New')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30182: Type expected.
        Dim i1, i2 As New'BIND:"Dim i1, i2 As New"
                         ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MixedDimAsNewAndEqualsDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1, i2 As New Integer, b1 As Boolean = False'BIND:"Dim i1, i2 As New Integer, b1 As Boolean = False"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2  ... ean = False')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub MixedDimAsNewAndEqualsDeclarationsReversedOrder()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim b1 As Boolean, i1, i2 As New Integer'BIND:"Dim b1 As Boolean, i1, i2 As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim b1 As B ... New Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: null
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'b1'.
        Dim b1 As Boolean, i1, i2 As New Integer'BIND:"Dim b1 As Boolean, i1, i2 As New Integer"
            ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationWithLength()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(2) As Integer'BIND:"Dim i1(2) As Integer"
    End Sub
End Module]]>.Value

Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1(2) As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1(2)')
    Variables: Local_1: i1 As System.Int32()
    Initializer: IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'i1(2)')
        Dimension Sizes(1):
            IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3) (Syntax: '2')
              Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '2')
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationMultipleVariables()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(), i2 As Integer'BIND:"Dim i1(), i2 As Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1(), i2 As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1()')
    Variables: Local_1: i1 As System.Int32()
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Dim i1(), i2 As Integer'BIND:"Dim i1(), i2 As Integer"
            ~~
BC42024: Unused local variable: 'i2'.
        Dim i1(), i2 As Integer'BIND:"Dim i1(), i2 As Integer"
                  ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ArrayDeclarationInvalidAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim i1(2) As New Integer'BIND:"Dim i1(2) As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1(2) As New Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1(2)')
    Variables: Local_1: i1 As System.Int32()
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32(), IsInvalid) (Syntax: 'As New Integer')
        Children(1):
            IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'New Integer')
              Arguments(0)
              Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30053: Arrays cannot be declared with 'New'.
        Dim i1(2) As New Integer'BIND:"Dim i1(2) As New Integer"
                     ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Using Statements"

        <CompilerTrait(CompilerFeature.IOperation)>
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'c1 As New C')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C) (Syntax: 'New C')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub


        <CompilerTrait(CompilerFeature.IOperation)>
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'c1 As C = New C')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'c1')
    Variables: Local_1: c1 As Program.C
    Initializer: IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C) (Syntax: 'New C')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Const Declarations"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As Integer = 1'BIND:"Const i1 As Integer = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 As Integer = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1 As Integer = 1'BIND:"Const i1 As Integer = 1"
              ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 = 1, i2 = 2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1 = 1, i2 = 2'BIND:"Const i1 = 1, i2 = 2"
              ~~
BC42099: Unused local constant: 'i2'.
        Const i1 = 1, i2 = 2'BIND:"Const i1 = 1, i2 = 2"
                      ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstAsNew()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As New Integer'BIND:"Const i1 As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 As New Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'i1')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30438: Constants must have a value.
        Const i1 As New Integer'BIND:"Const i1 As New Integer"
              ~~
BC30246: 'New' is not valid on a local constant declaration.
        Const i1 As New Integer'BIND:"Const i1 As New Integer"
                    ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstAsNewMultipleDeclarations()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1, i ... New Integer')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'i1')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30438: Constants must have a value.
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
              ~~
BC42099: Unused local constant: 'i1'.
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
              ~~
BC30438: Constants must have a value.
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
                  ~~
BC30246: 'New' is not valid on a local constant declaration.
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
                        ~~~
BC30246: 'New' is not valid on a local constant declaration.
        Const i1, i2 As New Integer'BIND:"Const i1, i2 As New Integer"
                        ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstSingleDeclarationNoType()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1'BIND:"Const i1 = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1 = 1'BIND:"Const i1 = 1"
              ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclarationsNoTypes()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1, i2 = 'BIND:"Const i1 = 1, i2 = "
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = 1, i2 = ')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1 = 1, i2 = 'BIND:"Const i1 = 1, i2 = "
              ~~
BC30201: Expression expected.
        Const i1 = 1, i2 = 'BIND:"Const i1 = 1, i2 = "
                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstSingleDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1
        Const i2 = i1'BIND:"Const i2 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i2 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i2'.
        Const i2 = i1'BIND:"Const i2 = i1"
              ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstMultipleDeclarationsLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1
        Const i2 = i1, i3 = i1'BIND:"Const i2 = i1, i3 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i2 = i1, i3 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Int32
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i2'.
        Const i2 = i1, i3 = i1'BIND:"Const i2 = i1, i3 = i1"
              ~~
BC42099: Unused local constant: 'i3'.
        Const i2 = i1, i3 = i1'BIND:"Const i2 = i1, i3 = i1"
                       ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = Int1()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Int1()')
        Children(1):
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
              Instance Receiver: null
              Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
        Const i1 = Int1()'BIND:"Const i1 = Int1()"
                   ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 =  ... i2 = Int1()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Int1()')
        Children(1):
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
              Instance Receiver: null
              Arguments(0)
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'Int1()')
        Children(1):
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
              Instance Receiver: null
              Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30059: Constant expression is required.
        Const i1 = Int1(), i2 = Int1()'BIND:"Const i1 = Int1(), i2 = Int1()"
                   ~~~~~~
BC30059: Constant expression is required.
        Const i1 = Int1(), i2 = Int1()'BIND:"Const i1 = Int1(), i2 = Int1()"
                                ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDimAsNewNoInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 As New'BIND:"Const i1 As New"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 As New')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'i1')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30246: 'New' is not valid on a local constant declaration.
        Const i1 As New'BIND:"Const i1 As New"
                    ~~~
BC30182: Type expected.
        Const i1 As New'BIND:"Const i1 As New"
                       ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstDimAsNewMultipleDeclarationsNoInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1, i2 As New')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New')
    Variables: Local_1: i1 As ?
      Local_2: i2 As ?
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'i1')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
              ~~
BC30246: 'New' is not valid on a local constant declaration.
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
                        ~~~
BC30246: 'New' is not valid on a local constant declaration.
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
                        ~~~
BC30182: Type expected.
        Const i1, i2 As New'BIND:"Const i1, i2 As New"
                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub ConstInvalidMultipleDeclaration()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Const i1 = 1,'BIND:"Const i1 = 1,"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = 1,')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42099: Unused local constant: 'i1'.
        Const i1 = 1,'BIND:"Const i1 = 1,"
              ~~
BC30203: Identifier expected.
        Const i1 = 1,'BIND:"Const i1 = 1,"
                     ~
BC30438: Constants must have a value.
        Const i1 = 1,'BIND:"Const i1 = 1,"
                     ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Static Declarations"

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Static i1 As Integer'BIND:"Static i1 As Integer"
               ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1, i2 As Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Static i1, i2 As Integer'BIND:"Static i1, i2 As Integer"
               ~~
BC42024: Unused local variable: 'i2'.
        Static i1, i2 As Integer'BIND:"Static i1, i2 As Integer"
                   ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 As New Integer')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1,  ... New Integer')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1,  ... ean = False')
  IVariableDeclaration (2 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
        Arguments(0)
        Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationNoType()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1'BIND:"Static i1 = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = 1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsNoTypes()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1, i2 = 2'BIND:"Static i1 = 1, i2 = 2"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = 1, i2 = 2')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticSingleDeclarationLocalReferenceInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1
        Static i2 = i1'BIND:"Static i2 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i2 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticMultipleDeclarationsLocalReferenceInitializers()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 = 1
        Static i2 = i1, i3 = i1'BIND:"Static i2 = i1, i3 = i1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i2 = i1, i3 = i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Object
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = Int1()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'Int1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
            Instance Receiver: null
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = ... i2 = Int1()')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'Int1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
            Instance Receiver: null
            Arguments(0)
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'Int1()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
            Instance Receiver: null
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub StaticAsNewSingleDeclarationInvalidInitializer()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Static i1 As'BIND:"Static i1 As"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 As')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Static i1 As'BIND:"Static i1 As"
               ~~
BC30182: Type expected.
        Static i1 As'BIND:"Static i1 As"
                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1, i2 As')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As ?
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Static i1, i2 As'BIND:"Static i1, i2 As"
               ~~
BC42024: Unused local variable: 'i2'.
        Static i1, i2 As'BIND:"Static i1, i2 As"
                   ~~
BC30182: Type expected.
        Static i1, i2 As'BIND:"Static i1, i2 As"
                        ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 =')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Static i1 ='BIND:"Static i1 ="
                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 =, i2 =')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
        Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Static i1 =, i2 ='BIND:"Static i1 =, i2 ="
                   ~
BC30201: Expression expected.
        Static i1 =, i2 ='BIND:"Static i1 =, i2 ="
                         ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1,')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: null
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
    Initializer: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'i1'.
        Static i1,'BIND:"Static i1,"
               ~~
BC30203: Identifier expected.
        Static i1,'BIND:"Static i1,"
                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region
    End Class
End Namespace
