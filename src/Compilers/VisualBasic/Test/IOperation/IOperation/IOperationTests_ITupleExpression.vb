' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  NaturalType: (System.Int32, System.Int32)
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (I ... r) = (1, 2)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (Integ ... r) = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (System.Int32, System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
          NaturalType: (System.Int32, System.Int32)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
  NaturalType: (System.Int32, System.Int32)
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (U ... r) = (1, 2)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (UInte ... r) = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (System.UInt32, System.UInt32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
        ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
          NaturalType: (System.Int32, System.Int32)
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
  NaturalType: null
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (U ... 1, Nothing)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (UInte ... 1, Nothing)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (System.UInt32, System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, Nothing)')
        ITupleOperation (OperationKind.Tuple, Type: (System.UInt32, System.String)) (Syntax: '(1, Nothing)')
          NaturalType: null
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(A:=1, B:=2)')
  NaturalType: (A As System.Int32, B As System.Int32)
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t = (A:=1, B:=2)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't = (A:=1, B:=2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (A As System.Int32, B As System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (A:=1, B:=2)')
        ITupleOperation (OperationKind.Tuple, Type: (A As System.Int32, B As System.Int32)) (Syntax: '(A:=1, B:=2)')
          NaturalType: (A As System.Int32, B As System.Int32)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  NaturalType: (System.Int32, System.Int32)
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (A ... r) = (1, 2)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (A As  ... r) = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (A As System.Int32, B As System.Int32)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (1, 2)')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (A As System.Int32, B As System.Int32), IsImplicit) (Syntax: '(1, 2)')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
              NaturalType: (System.Int32, System.Int32)
              Elements(2):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
  NaturalType: null
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (A ... B:=Nothing)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (A As  ... B:=Nothing)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (A As System.Int16, B As System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (A:=1, B:=Nothing)')
        ITupleOperation (OperationKind.Tuple, Type: (A As System.Int16, B As System.String)) (Syntax: '(A:=1, B:=Nothing)')
          NaturalType: null
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
ITupleOperation (OperationKind.Tuple, Type: (System.Int16, c1 As System.String)) (Syntax: '(New C(0), c1)')
  NaturalType: (C, c1 As C)
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.Int16) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'New C(0)')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.Int16)
        Operand: 
          IObjectCreationOperation (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(0)')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer: 
              null
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.String) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.String)
        Operand: 
          IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (A ... w C(0), c1)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (A As  ... w C(0), c1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (A As System.Int16, B As System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (New C(0), c1)')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (A As System.Int16, B As System.String), IsImplicit) (Syntax: '(New C(0), c1)')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ITupleOperation (OperationKind.Tuple, Type: (System.Int16, c1 As System.String)) (Syntax: '(New C(0), c1)')
              NaturalType: (C, c1 As C)
              Elements(2):
                  IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.Int16) (OperationKind.Conversion, Type: System.Int16, IsImplicit) (Syntax: 'New C(0)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.Int16)
                    Operand: 
                      IObjectCreationOperation (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(0)')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Initializer: 
                          null
                  IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.String) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.String)
                    Operand: 
                      IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
  NaturalType: null
  Elements(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As C  ... 0, Nothing)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As C = (0, Nothing)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (0, Nothing)')
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(x As (System.Int32, System.String)) As C) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: '(0, Nothing)')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(x As (System.Int32, System.String)) As C)
          Operand: 
            ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Object)) (Syntax: '(0, Nothing)')
              NaturalType: null
              Elements(2):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

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
IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
]]>.Value

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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim t As (I ... tring) = c1')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 't As (Integ ... tring) = c1')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (System.Int32, System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= c1')
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As (System.Int32, System.String)) (OperationKind.Conversion, Type: (System.Int32, System.String), IsImplicit) (Syntax: 'c1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As (System.Int32, System.String))
          Operand: 
            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
ITupleOperation (OperationKind.Tuple, Type: (System.Int16, c1 As System.String), IsInvalid) (Syntax: '(New C(0), c1)')
  NaturalType: (C, c1 As C)
  Elements(2):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'New C(0)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C(0)')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '0')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer: 
              null
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.String) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.String)
        Operand: 
          IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim t As (S ... w C(0), c1)')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 't As (Short ... w C(0), c1)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: t As (System.Int16, System.String)) (OperationKind.VariableDeclarator, Type: null) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (New C(0), c1)')
        ITupleOperation (OperationKind.Tuple, Type: (System.Int16, c1 As System.String), IsInvalid) (Syntax: '(New C(0), c1)')
          NaturalType: (C, c1 As C)
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int16, IsInvalid, IsImplicit) (Syntax: 'New C(0)')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IObjectCreationOperation (Constructor: Sub C..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C(0)')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: 
                      null
              IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: Function C.op_Implicit(c As C) As System.String) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'c1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: Function C.op_Implicit(c As C) As System.String)
                Operand: 
                  IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Short'.
        Dim t As (Short, String) = (New C(0), c1)'BIND:"Dim t As (Short, String) = (New C(0), c1)"
                                    ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TupleFlow_01()
            Dim source = <![CDATA[
Class C
    Sub M(b As Boolean)'BIND:"Sub M(b As Boolean)"
        Dim t As (Integer, Integer) = (1, If(b, 2, 3))
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [t As (System.Int32, System.Int32)]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't As (Integ ... f(b, 2, 3))')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, If(b, 2, 3))')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TupleFlow_02()
            Dim source = <![CDATA[
Class C
    Sub M(b As Boolean)'BIND:"Sub M(b As Boolean)"
        Dim t As (Integer, (Integer, Integer)) = (1, (2, If(b, 2, 3)))
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [t As (System.Int32, (System.Int32, System.Int32))]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 't As (Integ ... (b, 2, 3)))')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 't')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, If(b, 2, 3)))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, If(b, 2, 3))')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TupleFlow_03()
            Dim source = <![CDATA[
Class C
    Sub M(b As Boolean)'BIND:"Sub M(b As Boolean)"
        M2((1, If(b, 2, 3)))
    End Sub

    Sub M2(arg As (Integer, Integer))
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M2((1, If(b, 2, 3)))')
              Expression: 
                IInvocationOperation ( Sub C.M2(arg As (System.Int32, System.Int32))) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2((1, If(b, 2, 3)))')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'M2')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: '(1, If(b, 2, 3))')
                        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, If(b, 2, 3))')
                          NaturalType: (System.Int32, System.Int32)
                          Elements(2):
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TupleFlow_04()
            Dim source = <![CDATA[
Class C
    Sub M(b As Boolean)'BIND:"Sub M(b As Boolean)"
        Dim t As (Integer, Integer) = (If(b, 2, 3), 1)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [t As (System.Int32, System.Int32)]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't As (Integ ... , 2, 3), 1)')
              Left: 
                ILocalReferenceOperation: t (IsDeclaration: True) (OperationKind.LocalReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 't')
              Right: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(If(b, 2, 3), 1)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
