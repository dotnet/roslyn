' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NoConversions()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (Integer, Integer) = (1, 2)'BIND:"(1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  Elements(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NoConversions_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (Integer, Integer) = (1, 2)'BIND:"Dim t As (Integer, Integer) = (1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (I ... r) = (1, 2)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (System.Int32, System.Int32)
    Initializer: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
        Elements(2):
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_ImplicitConversions()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (UInteger, UInteger) = (1, 2)'BIND:"(1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
      Elements(2):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 2) (Syntax: '2')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_ImplicitConversions_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (UInteger, UInteger) = (1, 2)'BIND:"Dim t As (UInteger, UInteger) = (1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (U ... r) = (1, 2)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (System.UInt32, System.UInt32)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
            Elements(2):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 2) (Syntax: '2')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_ImplicitConversionFromNull()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (UInteger, String) = (1, Nothing)'BIND:"(1, Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
      Elements(2):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_ImplicitConversionFromNull_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim t As (UInteger, String) = (1, Nothing)'BIND:"Dim t As (UInteger, String) = (1, Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (U ... 1, Nothing)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (System.UInt32, System.String)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
            Elements(2):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedArguments()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t = (A:=1, B:=2)'BIND:"(A:=1, B:=2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITupleExpression (OperationKind.TupleExpression, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(A:=1, B:=2)')
  Elements(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedArguments_ParentVariableDeclaration()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t = (A:=1, B:=2)'BIND:"Dim t = (A:=1, B:=2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t = (A:=1, B:=2)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (A As System.Int32, B As System.Int32)
    Initializer: ITupleExpression (OperationKind.TupleExpression, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(A:=1, B:=2)')
        Elements(2):
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedElementsInTupleType()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t As (A As Integer, B As Integer) = (1, 2)'BIND:"(1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(1, 2)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      Elements(2):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedElementsInTupleType_ParentVariableDeclaration()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t As (A As Integer, B As Integer) = (1, 2)'BIND:"Dim t As (A As Integer, B As Integer) = (1, 2)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (A ... r) = (1, 2)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (A As System.Int32, B As System.Int32)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(1, 2)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
            Elements(2):
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedElementsAndImplicitConversions()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t As (A As Int16, B As String) = (A:=1, B:=Nothing)'BIND:"(A:=1, B:=Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ITupleExpression (OperationKind.TupleExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
      Elements(2):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1) (Syntax: '1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_NamedElementsAndImplicitConversions_ParentVariableDeclaration()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class C
    Shared Sub Main()
        Dim t As (A As Int16, B As String) = (A:=1, B:=Nothing)'BIND:"Dim t As (A As Int16, B As String) = (A:=1, B:=Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (A ... B:=Nothing)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (A As System.Int16, B As System.String)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITupleExpression (OperationKind.TupleExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
            Elements(2):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1) (Syntax: '1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionsForArguments()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(value As Integer) As C
        Return New C(value)
    End Operator

    Public Shared Widening Operator CType(c As C) As Short
        Return CShort(c._x)
    End Operator

    Public Shared Widening Operator CType(c As C) As String
        Return c._x.ToString()
    End Operator

    Public Sub M(c1 As C)
        Dim t As (A As Int16, B As String) = (New C(0), c1)'BIND:"(New C(0), c1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(New C(0), c1)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, c1 As System.String)) (Syntax: '(New C(0), c1)')
      Elements(2):
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16) (Syntax: 'New C(0)')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'New C(0)')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IObjectCreationExpression (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'New C(0)')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                    Initializer: null
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionsForArguments_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(value As Integer) As C
        Return New C(value)
    End Operator

    Public Shared Widening Operator CType(c As C) As Short
        Return CShort(c._x)
    End Operator

    Public Shared Widening Operator CType(c As C) As String
        Return c._x.ToString()
    End Operator

    Public Sub M(c1 As C)
        Dim t As (A As Int16, B As String) = (New C(0), c1)'BIND:"Dim t As (A As Int16, B As String) = (New C(0), c1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (A ... w C(0), c1)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (A As System.Int16, B As System.String)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (A As System.Int16, B As System.String)) (Syntax: '(New C(0), c1)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, c1 As System.String)) (Syntax: '(New C(0), c1)')
            Elements(2):
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16) (Syntax: 'New C(0)')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'New C(0)')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IObjectCreationExpression (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'New C(0)')
                          Arguments(1):
                              IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          Initializer: null
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionFromTupleExpression()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(x As (Integer, String)) As C
        Return New C(x.Item1)
    End Operator

    Public Shared Widening Operator CType(c As C) As (Integer, String)
        Return (c._x, c._x.ToString)
    End Operator

    Public Sub M(c1 As C)
        Dim t As C = (0, Nothing)'BIND:"(0, Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: '(0, Nothing)')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
              Elements(2):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionFromTupleExpression_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(x As (Integer, String)) As C
        Return New C(x.Item1)
    End Operator

    Public Shared Widening Operator CType(c As C) As (Integer, String)
        Return (c._x, c._x.ToString)
    End Operator

    Public Sub M(c1 As C)
        Dim t As C = (0, Nothing)'BIND:"Dim t As C = (0, Nothing)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As C  ... 0, Nothing)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As C
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: '(0, Nothing)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
                    Elements(2):
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionToTupleType()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(x As (Integer, String)) As C
        Return New C(x.Item1)
    End Operator

    Public Shared Widening Operator CType(c As C) As (Integer, String)
        Return (c._x, c._x.ToString)
    End Operator

    Public Sub M(c1 As C)
        Dim t As (Integer, String) = c1'BIND:"c1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.String)) (Syntax: 'c1')
  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_UserDefinedConversionToTupleType_ParentVariableDeclaration()
            Dim source = <![CDATA[
Imports System

Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(x As (Integer, String)) As C
        Return New C(x.Item1)
    End Operator

    Public Shared Widening Operator CType(c As C) As (Integer, String)
        Return (c._x, c._x.ToString)
    End Operator

    Public Sub M(c1 As C)
        Dim t As (Integer, String) = c1'BIND:"Dim t As (Integer, String) = c1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim t As (I ... tring) = c1')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (System.Int32, System.String)
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: (System.Int32, System.String)) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_InvalidConversion()
            Dim source = <![CDATA[
Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(value As Integer) As C
        Return New C(value)
    End Operator

    Public Shared Widening Operator CType(c As C) As Integer
        Return CShort(c._x)
    End Operator

    Public Shared Widening Operator CType(c As C) As String
        Return c._x.ToString()
    End Operator

    Public Sub M(c1 As C)
        Dim t As (Short, String) = (New C(0), c1)'BIND:"(New C(0), c1)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, c1 As System.String), IsInvalid) (Syntax: '(New C(0), c1)')
  Elements(2):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, IsInvalid) (Syntax: 'New C(0)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IObjectCreationExpression (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'New C(0)')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsInvalid) (Syntax: '0')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
            Initializer: null
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Short'.
        Dim t As (Short, String) = (New C(0), c1)'BIND:"(New C(0), c1)"
                                    ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")>
        Public Sub TupleExpression_InvalidConversion_ParentVariableDeclaration()
            Dim source = <![CDATA[
Class C
    Private ReadOnly _x As Integer
    Public Sub New(x As Integer)
        _x = x
    End Sub

    Public Shared Widening Operator CType(value As Integer) As C
        Return New C(value)
    End Operator

    Public Shared Widening Operator CType(c As C) As Integer
        Return CShort(c._x)
    End Operator

    Public Shared Widening Operator CType(c As C) As String
        Return c._x.ToString()
    End Operator

    Public Sub M(c1 As C)
        Dim t As (Short, String) = (New C(0), c1)'BIND:"Dim t As (Short, String) = (New C(0), c1)"
    End Sub
End Class

]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim t As (S ... w C(0), c1)')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 't')
    Variables: Local_1: t As (System.Int16, System.String)
    Initializer: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, c1 As System.String), IsInvalid) (Syntax: '(New C(0), c1)')
        Elements(2):
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, IsInvalid) (Syntax: 'New C(0)')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: IObjectCreationExpression (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreationExpression, Type: C, IsInvalid) (Syntax: 'New C(0)')
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, IsInvalid) (Syntax: '0')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  Initializer: null
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: C) (Syntax: 'c1')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Short'.
        Dim t As (Short, String) = (New C(0), c1)'BIND:"Dim t As (Short, String) = (New C(0), c1)"
                                    ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
