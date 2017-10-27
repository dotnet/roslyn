' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Operations
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1 As Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
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
IVariableDeclarationsOperation (3 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1 As I ...  As Boolean')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1, i2')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim i1 As Integer,')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim i1,')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i2 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i2 = i1, i3 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1 = ReturnInt()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ReturnInt()')
        IInvocationOperation (Function Program.ReturnInt() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ReturnInt()')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1 = Re ... ReturnInt()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ReturnInt()')
        IInvocationOperation (Function Program.ReturnInt() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ReturnInt()')
          Instance Receiver: 
            null
          Arguments(0)
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ReturnInt()')
        IInvocationOperation (Function Program.ReturnInt() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'ReturnInt()')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim p1 As New C')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'p1')
    Variables: Local_1: p1 As Program.C
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
        IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1, i2  ... New Integer')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim i1 As New')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'New')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim i1, i2 As New')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1, i2 As New')
    Variables: Local_1: i1 As ?
      Local_2: i2 As ?
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'New')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1, i2  ... ean = False')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= False')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim b1 As B ... New Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: 
      null
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1(2) As Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1(2)')
    Variables: Local_1: i1 As System.Int32()
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsImplicit) (Syntax: 'i1(2)')
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: 'i1(2)')
          Dimension Sizes(1):
              IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '2')
                Left: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim i1(), i2 As Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1()')
    Variables: Local_1: i1 As System.Int32()
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim i1(2) As New Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1(2)')
    Variables: Local_1: i1 As System.Int32()
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New Integer')
        IInvalidOperation (OperationKind.Invalid, Type: System.Int32(), IsInvalid, IsImplicit) (Syntax: 'As New Integer')
          Children(1):
              IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32, IsInvalid) (Syntax: 'New Integer')
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
IUsingOperation (OperationKind.Using, Type: null) (Syntax: 'Using c1 As ... End Using')
  Resources: 
    IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Using c1 As New C')
      IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'c1')
        Variables: Local_1: c1 As Program.C
        Initializer: 
          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New C')
            IObjectCreationOperation (Constructor: Sub Program.C..ctor()) (OperationKind.ObjectCreation, Type: Program.C) (Syntax: 'New C')
              Arguments(0)
              Initializer: 
                null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Using c1 As ... End Using')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(c1)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(c1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c1')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: Program.C) (Syntax: 'c1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Const i1 As Integer = 1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Const i1 = 1, i2 = 2')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 As New Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New Integer')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1, i ... New Integer')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New Integer')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Const i1 = 1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 = 1, i2 = ')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= ')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Const i2 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Const i2 = i1, i3 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, Constant: 1) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 = Int1()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Int1()')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 =  ... i2 = Int1()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Int1()')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
                Instance Receiver: 
                  null
                Arguments(0)
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= Int1()')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Int1()')
          Children(1):
              IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'Int1()')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 As New')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1, i2 As New')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'i1, i2 As New')
    Variables: Local_1: i1 As ?
      Local_2: i2 As ?
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: 'As New')
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'i1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Const i1 = 1,')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid, IsImplicit) (Syntax: '')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: '')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 As Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1, i2 As Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Int32
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 As New Integer')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1,  ... New Integer')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1,  ... ean = False')
  IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1, i2 As New Integer')
    Variables: Local_1: i1 As System.Int32
      Local_2: i2 As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Integer')
        IObjectCreationOperation (Constructor: Sub System.Int32..ctor()) (OperationKind.ObjectCreation, Type: System.Int32) (Syntax: 'New Integer')
          Arguments(0)
          Initializer: 
            null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'b1')
    Variables: Local_1: b1 As System.Boolean
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= False')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 = 1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 = 1, i2 = 2')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 2')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i2 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'i1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i2 = i1, i3 = i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'i1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i3')
    Variables: Local_1: i3 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i1')
        ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'i1')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 = Int1()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Int1()')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Int1()')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Static i1 = ... i2 = Int1()')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Int1()')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Int1()')
              Instance Receiver: 
                null
              Arguments(0)
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= Int1()')
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Int1()')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IInvocationOperation (Function Program.Int1() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Int1()')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Static i1 As')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Static i1, i2 As')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As ?
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As ?
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Static i1 =')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Static i1 =, i2 =')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
          Children(0)
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i2')
    Variables: Local_1: i2 As System.Object
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '=')
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Static i1,')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'i1')
    Variables: Local_1: i1 As System.Object
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '')
    Variables: Local_1:  As System.Object
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
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 1')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IVariableDeclarationsOperation (2 declarations) (OperationKind.VariableDeclarations, Type: null, IsInvalid) (Syntax: 'Dim x, y = 1')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'x')
    Variables: Local_1: x As System.Object
    Initializer: 
      null
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'y')
    Variables: Local_1: y As System.Int32
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= 1')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
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
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Test')
  IObjectCreationOperation (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreation, Type: Test) (Syntax: 'New Test')
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
IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, Type: null) (Syntax: 'Dim x As New Test')
  IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x')
    Variables: Local_1: x As Test
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Test')
        IObjectCreationOperation (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreation, Type: Test) (Syntax: 'New Test')
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
IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Test')
  IObjectCreationOperation (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreation, Type: Test) (Syntax: 'New Test')
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
IVariableDeclarationOperation (2 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x, y As New Test')
  Variables: Local_1: x As Test
    Local_2: y As Test
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: 'As New Test')
      IObjectCreationOperation (Constructor: Sub Test..ctor()) (OperationKind.ObjectCreation, Type: Test) (Syntax: 'New Test')
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
