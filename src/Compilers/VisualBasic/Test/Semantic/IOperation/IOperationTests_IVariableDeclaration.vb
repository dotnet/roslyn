' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (3 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 As I ...  As Boolean')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'b1 As Boolean')
    Declarations:
        ISingleVariableDeclaration (Symbol: b1 As System.Boolean) (OperationKind.SingleVariableDeclaration) (Syntax: 'b1')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As Integer,')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: '')
    Declarations:
        ISingleVariableDeclaration (Symbol:  As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1,')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1,')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol:  As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i2 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i2 = i1, i3 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i3 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i3 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i3')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 = ReturnInt()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = ReturnInt()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= ReturnInt()')
        IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
          Instance Receiver: 
            null
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1 = Re ... ReturnInt()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = ReturnInt()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= ReturnInt()')
        IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
          Instance Receiver: 
            null
          Arguments(0)
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = ReturnInt()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= ReturnInt()')
        IInvocationExpression (Function Program.ReturnInt() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'ReturnInt()')
          Instance Receiver: 
            null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim p1 As New C')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'p1 As New C')
    Declarations:
        ISingleVariableDeclaration (Symbol: p1 As Program.C) (OperationKind.SingleVariableDeclaration) (Syntax: 'p1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New C')
        IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C) (Syntax: 'New C')
          Arguments(0)
          Initializer: 
            null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2  ... New Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1 As New')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 As New')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'New')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1, i2 As New')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'New')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1, i2  ... ean = False')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'b1 As Boolean = False')
    Declarations:
        ISingleVariableDeclaration (Symbol: b1 As System.Boolean) (OperationKind.SingleVariableDeclaration) (Syntax: 'b1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= False')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
]]>.Value

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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim b1 As B ... New Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'b1 As Boolean')
    Declarations:
        ISingleVariableDeclaration (Symbol: b1 As System.Boolean) (OperationKind.SingleVariableDeclaration) (Syntax: 'b1')
          Initializer: 
            null
    Initializer: 
      null
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1(2) As Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1(2) As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1(2)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'i1(2)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'i1(2)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim i1(), i2 As Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1(), i2 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1()')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim i1(2) As New Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1(2) As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1(2)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'i1(2)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'i1(2)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New Integer')
        IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32(), IsInvalid) (Syntax: 'As New Integer')
          Children(1):
              IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'New Integer')
                Arguments(0)
                Initializer: 
                  null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30053: Arrays cannot be declared with 'New'.
        Dim i1(2) As New Integer'BIND:"Dim i1(2) As New Integer"
                     ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(22362, "https://github.com/dotnet/roslyn/issues/22362")>
        Public Sub ArrayRangeDeclaration()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim a(0 To 4) As Integer'BIND:"a(0 To 4) As Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'a(0 To 4) As Integer')
  Declarations:
      ISingleVariableDeclaration (Symbol: a As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'a(0 To 4)')
        Initializer: 
          IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'a(0 To 4)')
            IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'a(0 To 4)')
              Dimension Sizes(1):
                  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 5, IsImplicit) (Syntax: '0 To 4')
                    Left: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '0 To 4')
              Initializer: 
                null
  Initializer: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(22362, "https://github.com/dotnet/roslyn/issues/22362")>
        Public Sub ArrayDeclarationCollectionInitializer()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim s As String() = {"Hello", "World"}'BIND:"s As String() = {"Hello", "World"}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 's As String ... ", "World"}')
  Declarations:
      ISingleVariableDeclaration (Symbol: s As System.String()) (OperationKind.SingleVariableDeclaration) (Syntax: 's')
        Initializer: 
          null
  Initializer: 
    IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= {"Hello", "World"}')
      IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: '{"Hello", "World"}')
        Dimension Sizes(1):
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{"Hello", "World"}')
        Initializer: 
          IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: '{"Hello", "World"}')
            Element Values(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "World") (Syntax: '"World"')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(22362, "https://github.com/dotnet/roslyn/issues/22362")>
        Public Sub PercentTypeSpecifierWithNullableAndInitializer()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim d%? = 42'BIND:"d%? = 42"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'd%? = 42')
  Declarations:
      ISingleVariableDeclaration (Symbol: d As System.Nullable(Of System.Int32)) (OperationKind.SingleVariableDeclaration) (Syntax: 'd%?')
        Initializer: 
          null
  Initializer: 
    IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 42')
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '42')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 42) (Syntax: '42')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(22362, "https://github.com/dotnet/roslyn/issues/22362")>
        Public Sub MultipleIdentifiersWithSingleInitializer_Invalid()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim d, x%? = 42'BIND:"d, x%? = 42"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'd, x%? = 42')
  Declarations:
      ISingleVariableDeclaration (Symbol: d As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'd')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: x As System.Nullable(Of System.Int32)) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'x%?')
        Initializer: 
          null
  Initializer: 
    IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= 42')
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '42')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Dim d, x%? = 42'BIND:"d, x%? = 42"
            ~
BC42024: Unused local variable: 'd'.
        Dim d, x%? = 42'BIND:"d, x%? = 42"
            ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        Dim d, x%? = 42'BIND:"d, x%? = 42"
            ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub MultipleIdentifiersWithSingleInitializer_Invalid_ManyIdentifiers()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (26 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'a, b, c, d, ... Integer = 1')
  Declarations:
      ISingleVariableDeclaration (Symbol: a As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'a')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: b As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'b')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: c As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'c')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: d As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'd')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: e As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'e')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: f As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'f')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: g As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'g')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: h As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'h')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: i As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'i')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: j As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'j')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: k As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'k')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: l As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'l')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: m As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'm')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: n As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'n')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: o As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'o')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: p As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'p')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: q As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'q')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: r As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'r')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: s As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 's')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: t As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 't')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: u As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'u')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: v As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'v')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: w As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'w')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: x As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'x')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'y')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: z As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'z')
        Initializer: 
          null
  Initializer: 
    IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= 1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'a'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
            ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42024: Unused local variable: 'b'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
               ~
BC42024: Unused local variable: 'c'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                  ~
BC42024: Unused local variable: 'd'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                     ~
BC42024: Unused local variable: 'e'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                        ~
BC42024: Unused local variable: 'f'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                           ~
BC42024: Unused local variable: 'g'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                              ~
BC42024: Unused local variable: 'h'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                 ~
BC42024: Unused local variable: 'i'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                    ~
BC42024: Unused local variable: 'j'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                       ~
BC42024: Unused local variable: 'k'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                          ~
BC42024: Unused local variable: 'l'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                             ~
BC42024: Unused local variable: 'm'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                ~
BC42024: Unused local variable: 'n'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                   ~
BC42024: Unused local variable: 'o'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                      ~
BC42024: Unused local variable: 'p'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                         ~
BC42024: Unused local variable: 'q'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                            ~
BC42024: Unused local variable: 'r'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                               ~
BC42024: Unused local variable: 's'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                  ~
BC42024: Unused local variable: 't'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                     ~
BC42024: Unused local variable: 'u'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                        ~
BC42024: Unused local variable: 'v'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                           ~
BC42024: Unused local variable: 'w'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                              ~
BC42024: Unused local variable: 'x'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                                 ~
BC42024: Unused local variable: 'y'.
        Dim a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1'BIND:"a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z As Integer = 1"
                                                                                    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SingleIdentifierArray_Initializer()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim x() As Integer = New Integer() {1, 2, 3, 4}'BIND:"Dim x() As Integer = New Integer() {1, 2, 3, 4}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x() As  ... 1, 2, 3, 4}')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x() As Inte ... 1, 2, 3, 4}')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'x()')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= New Integ ... 1, 2, 3, 4}')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer ... 1, 2, 3, 4}')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: 'New Integer ... 1, 2, 3, 4}')
          Initializer: 
            IArrayInitializer (4 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2, 3, 4}')
              Element Values(4):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SingleIdentifier_ArrayBoundsAndAsNew()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim x(1) As New Integer'BIND:"Dim x(1) As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim x(1) As New Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'x(1) As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'x(1)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'x(1)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'x(1)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Initializer: 
                  null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New Integer')
        IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32(), IsInvalid) (Syntax: 'As New Integer')
          Children(1):
              IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'New Integer')
                Arguments(0)
                Initializer: 
                  null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30053: Arrays cannot be declared with 'New'.
        Dim x(1) As New Integer'BIND:"Dim x(1) As New Integer"
                    ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub SingleIdentifier_ArrayBoundsAndInitializer()
            Dim source = <![CDATA[
Option Strict On
Imports System.Text

Module M1
    Sub Sub1()
        Dim x(1) As Integer = New Integer() {1, 2, 3, 4}'BIND:"Dim x(1) As Integer = New Integer() {1, 2, 3, 4}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim x(1) As ... 1, 2, 3, 4}')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'x(1) As Int ... 1, 2, 3, 4}')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'x(1)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsInvalid, IsImplicit) (Syntax: 'x(1)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32(), IsInvalid) (Syntax: 'x(1)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2, IsInvalid, IsImplicit) (Syntax: '1')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
                Initializer: 
                  null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= New Integ ... 1, 2, 3, 4}')
        IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer ... 1, 2, 3, 4}')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: 'New Integer ... 1, 2, 3, 4}')
          Initializer: 
            IArrayInitializer (4 elements) (OperationKind.ArrayInitializer) (Syntax: '{1, 2, 3, 4}')
              Element Values(4):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        Dim x(1) As Integer = New Integer() {1, 2, 3, 4}'BIND:"Dim x(1) As Integer = New Integer() {1, 2, 3, 4}"
            ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub MultipleIdentifiers_ArrayAndAsNew()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Sub Sub1()
        Dim x(1), y(2) As New Integer'BIND:"Dim x(1), y(2) As New Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim x(1), y ... New Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'x(1), y(2)  ... New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'x(1)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'x(1)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'x(1)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '1')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Initializer: 
                  null
        ISingleVariableDeclaration (Symbol: y As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'y(2)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'y(2)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'y(2)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New Integer')
        IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32(), IsInvalid) (Syntax: 'As New Integer')
          Children(1):
              IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32, IsInvalid) (Syntax: 'New Integer')
                Arguments(0)
                Initializer: 
                  null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30053: Arrays cannot be declared with 'New'.
        Dim x(1), y(2) As New Integer'BIND:"Dim x(1), y(2) As New Integer"
                          ~~~
BC30053: Arrays cannot be declared with 'New'.
        Dim x(1), y(2) As New Integer'BIND:"Dim x(1), y(2) As New Integer"
                          ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub MultipleIdentifiers_MixedArrayAndNonArray()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Sub Sub1()
        Dim x(10), y As Integer'BIND:"Dim x(10), y As Integer"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x(10), y As Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x(10), y As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'x(10)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'x(10)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'x(10)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 11, IsImplicit) (Syntax: '10')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '10')
                Initializer: 
                  null
        ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
          Initializer: 
            null
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'y'.
        Dim x(10), y As Integer'BIND:"Dim x(10), y As Integer"
                   ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub MultipleIdentifiers_MultipleArrays()
            Dim source = <![CDATA[
Option Strict On

Module M1
    Sub Sub1()
        Dim x%(10), y$(11)'BIND:"Dim x%(10), y$(11)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x%(10), y$(11)')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x%(10), y$(11)')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Int32()) (OperationKind.SingleVariableDeclaration) (Syntax: 'x%(10)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'x%(10)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'x%(10)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 11, IsImplicit) (Syntax: '10')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '10')
                Initializer: 
                  null
        ISingleVariableDeclaration (Symbol: y As System.String()) (OperationKind.SingleVariableDeclaration) (Syntax: 'y$(11)')
          Initializer: 
            IVariableInitializer (OperationKind.VariableInitializer, IsImplicit) (Syntax: 'y$(11)')
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.String()) (Syntax: 'y$(11)')
                Dimension Sizes(1):
                    IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, Constant: 12, IsImplicit) (Syntax: '11')
                      Left: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 11) (Syntax: '11')
                      Right: 
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '11')
                Initializer: 
                  null
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

#End Region

#Region "Using Statements"

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")>
        Public Sub UsingStatementDeclarationAsNew()
            Dim source = <![CDATA[
Imports System

Module Program
    Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Sub Main(args As String())
        Using c1 As New C'BIND:"Using c1 As New C"
            Console.WriteLine(c1)
        End Using
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IUsingStatement (OperationKind.UsingStatement) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Using c1 As New C')
      IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'c1 As New C')
        Declarations:
            ISingleVariableDeclaration (Symbol: c1 As Program.C) (OperationKind.SingleVariableDeclaration) (Syntax: 'c1')
              Initializer: 
                null
        Initializer: 
          IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New C')
            IObjectCreationExpression (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreationExpression, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c1')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceExpression: c1 (OperationKind.LocalReferenceExpression, Type: Program.C) (Syntax: 'c1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UsingBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 As Integer = 1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As Integer = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 = 1, i2 = 2')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = 2')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 2')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 As New Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New Integer')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1, i ... New Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New Integer')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i1 = 1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = 1, i2 = ')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i2 = ')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= ')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i2 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Const i2 = i1, i3 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i3 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i3 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i3')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = Int1()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= Int1()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
                Instance Receiver: 
                  null
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 =  ... i2 = Int1()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= Int1()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
                Instance Receiver: 
                  null
                Arguments(0)
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i2 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= Int1()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
                Instance Receiver: 
                  null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 As New')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 As New')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1, i2 As New')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As New')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: 'As New')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: 'i1')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Const i1 = 1,')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: '')
    Declarations:
        ISingleVariableDeclaration (Symbol:  As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid, IsImplicit) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 As Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1, i2 As Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 As New Integer')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1,  ... New Integer')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1,  ... ean = False')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1, i2 As New Integer')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As System.Int32) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Integer')
        IObjectCreationExpression (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'b1 As Boolean = False')
    Declarations:
        ISingleVariableDeclaration (Symbol: b1 As System.Boolean) (OperationKind.SingleVariableDeclaration) (Syntax: 'b1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= False')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'False')
]]>.Value

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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = 1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = 1, i2 = 2')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = 2')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 2')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: '2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i2 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i2 = i1, i3 = i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i3 = i1')
    Declarations:
        ISingleVariableDeclaration (Symbol: i3 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i3')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'i1')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = Int1()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= Int1()')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
              Instance Receiver: 
                null
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Static i1 = ... i2 = Int1()')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i1 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= Int1()')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
              Instance Receiver: 
                null
              Arguments(0)
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'i2 = Int1()')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= Int1()')
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationExpression (Function Program.Int1() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Int1()')
              Instance Receiver: 
                null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 As')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 As')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1, i2 As')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1, i2 As')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: i2 As ?) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      null
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 =')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 =')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '=')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroup (2 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1 =, i2 =')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1 =')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '=')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
          Children(0)
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i2 =')
    Declarations:
        ISingleVariableDeclaration (Symbol: i2 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i2')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '=')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Static i1,')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'i1,')
    Declarations:
        ISingleVariableDeclaration (Symbol: i1 As System.Object) (OperationKind.SingleVariableDeclaration) (Syntax: 'i1')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol:  As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: '')
          Initializer: 
            null
    Initializer: 
      null
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

#Region "Initializers"
        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForEqualsValueVariableInitializer()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x = 1'BIND:"= 1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializer (OperationKind.VariableInitializer) (Syntax: '= 1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForEqualsValueVariableInitializerWithMultipleLocals()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x, y = 1'BIND:"= 1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= 1')
  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'x'.
        Dim x, y = 1'BIND:"= 1"
            ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        Dim x, y = 1'BIND:"= 1"
            ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of EqualsValueSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForEqualsValueVariableDeclarationWithMultipleLocals()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x, y = 1'BIND:"Dim x, y = 1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim x, y = 1')
  IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration, IsInvalid) (Syntax: 'x, y = 1')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As System.Object) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'x')
          Initializer: 
            null
        ISingleVariableDeclaration (Symbol: y As System.Int32) (OperationKind.SingleVariableDeclaration, IsInvalid) (Syntax: 'y')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer, IsInvalid) (Syntax: '= 1')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42024: Unused local variable: 'x'.
        Dim x, y = 1'BIND:"Dim x, y = 1"
            ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        Dim x, y = 1'BIND:"Dim x, y = 1"
            ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForAsNewVariableInitializer()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x As New Test'BIND:"As New Test"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Test')
  IObjectCreationExpression (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test) (Syntax: 'New Test')
    Arguments(0)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForAsNewVariableDeclaration()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x As New Test'BIND:"Dim x As New Test"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationGroup (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim x As New Test')
  IMultiVariableDeclaration (1 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x As New Test')
    Declarations:
        ISingleVariableDeclaration (Symbol: x As Test) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
          Initializer: 
            null
    Initializer: 
      IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Test')
        IObjectCreationExpression (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test) (Syntax: 'New Test')
          Arguments(0)
          Initializer: 
            null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForAsNewVariableInitializerWithMultipleLocals()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x, y As New Test'BIND:"As New Test"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Test')
  IObjectCreationExpression (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test) (Syntax: 'New Test')
    Arguments(0)
    Initializer: 
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AsNewClauseSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TestGetOperationForAsNewVariableDeclarationWithMultipleLocals()
            Dim source = <![CDATA[
Class Test
    Sub M()
        Dim x, y As New Test'BIND:"x, y As New Test"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IMultiVariableDeclaration (2 declarations) (OperationKind.MultiVariableDeclaration) (Syntax: 'x, y As New Test')
  Declarations:
      ISingleVariableDeclaration (Symbol: x As Test) (OperationKind.SingleVariableDeclaration) (Syntax: 'x')
        Initializer: 
          null
      ISingleVariableDeclaration (Symbol: y As Test) (OperationKind.SingleVariableDeclaration) (Syntax: 'y')
        Initializer: 
          null
  Initializer: 
    IVariableInitializer (OperationKind.VariableInitializer) (Syntax: 'As New Test')
      IObjectCreationExpression (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreationExpression, Type: Test) (Syntax: 'New Test')
        Arguments(0)
        Initializer: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of VariableDeclaratorSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
#End Region
    End Class
End Namespace
