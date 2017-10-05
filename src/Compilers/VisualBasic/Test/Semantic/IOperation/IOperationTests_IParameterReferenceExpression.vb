' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_TupleExpression()
            Dim source = <![CDATA[
Class Class1
    Public Sub M(x As Integer, y As Integer)
        Dim tuple = (x, x + y)'BIND:"(x, x + y)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITupleExpression (OperationKind.TupleExpression, Type: (x As System.Int32, System.Int32)) (Syntax: '(x, x + y)')
  Elements(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
        Left: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: 
          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TupleExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_AnonymousObjectCreation()
            Dim source = <![CDATA[
Class Class1
    Public Sub M(x As Integer, y As String)
        Dim v = New With {'BIND:"New With {"'BIND:"New With {'BIND:"New With {""
            Key .Amount = x,
            Key .Message = "Hello" + y
        }
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key Amount As System.Int32, Key Message As System.String>) (Syntax: 'New With {' ... }')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'Key .Amount = x')
        Left: 
          IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key Amount As System.Int32, Key Message As System.String>.Amount As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'Amount')
            Instance Receiver: 
              null
        Right: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: 'Key .Messag ... "Hello" + y')
        Left: 
          IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key Amount As System.Int32, Key Message As System.String>.Message As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Message')
            Instance Receiver: 
              null
        Right: 
          IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: '"Hello" + y')
            Left: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
            Right: 
              IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_QueryExpression()
            Dim source = <![CDATA[
Imports System.Linq
Imports System.Collections.Generic

Structure Customer
    Public Property Name As String
    Public Property Address As String
End Structure

Class Class1
    Public Sub M(customers As List(Of Customer))
        Dim result = From cust In customers'BIND:"From cust In customers"
                     Select cust.Name
    End Sub
End Class

]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable(Of System.String)) (Syntax: 'From cust I ... t cust.Name')
  Expression: 
    IInvocationExpression ( Function System.Collections.Generic.IEnumerable(Of Customer).Select(Of System.String)(selector As System.Func(Of Customer, System.String)) As System.Collections.Generic.IEnumerable(Of System.String)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'Select cust.Name')
      Instance Receiver: 
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of Customer)) (Syntax: 'cust In customers')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceExpression: customers (OperationKind.ParameterReferenceExpression, Type: System.Collections.Generic.List(Of Customer)) (Syntax: 'customers')
      Arguments(1):
          IArgument (ArgumentKind.DefaultValue, Matching Parameter: selector) (OperationKind.Argument, IsImplicit) (Syntax: 'cust.Name')
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of Customer, System.String), IsImplicit) (Syntax: 'cust.Name')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IAnonymousFunctionExpression (Symbol: Function (cust As Customer) As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'cust.Name')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'cust.Name')
                    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'cust.Name')
                      ReturnedValue: 
                        IPropertyReferenceExpression: Property Customer.Name As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'cust.Name')
                          Instance Receiver: 
                            IParameterReferenceExpression: cust (OperationKind.ParameterReferenceExpression, Type: Customer) (Syntax: 'cust.Name')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_QueryExpressionAggregateClause()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Class C
    Public Sub Method(x As Integer)
        Console.WriteLine(Aggregate y In New Integer() {x} Into Count())'BIND:"Aggregate y In New Integer() {x} Into Count()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Int32) (Syntax: 'Aggregate y ... nto Count()')
  Expression: 
    IInvocationExpression ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
      Instance Receiver: 
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of System.Int32)) (Syntax: 'y In New Integer() {x}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: 'New Integer() {x}')
              Dimension Sizes(1):
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {x}')
              Initializer: 
                IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{x}')
                  Element Values(1):
                      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_QueryExpressionOrderByClause()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Class C
    Public Sub Method(x As String())
        Console.WriteLine(From y In x Order By y.Length)'BIND:"From y In x Order By y.Length"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'From y In x ... By y.Length')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: 
    ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Linq.IOrderedEnumerable(Of System.String)) (Syntax: 'From y In x ... By y.Length')
      Expression: 
        IInvocationExpression ( Function System.Collections.Generic.IEnumerable(Of System.String).OrderBy(Of System.Int32)(keySelector As System.Func(Of System.String, System.Int32)) As System.Linq.IOrderedEnumerable(Of System.String)) (OperationKind.InvocationExpression, Type: System.Linq.IOrderedEnumerable(Of System.String), IsImplicit) (Syntax: 'y.Length')
          Instance Receiver: 
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of System.String)) (Syntax: 'y In x')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String()) (Syntax: 'x')
          Arguments(1):
              IArgument (ArgumentKind.DefaultValue, Matching Parameter: keySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'y.Length')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String, System.Int32), IsImplicit) (Syntax: 'y.Length')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IAnonymousFunctionExpression (Symbol: Function (y As System.String) As System.Int32) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'y.Length')
                      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'y.Length')
                        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'y.Length')
                          ReturnedValue: 
                            IPropertyReferenceExpression: ReadOnly Property System.String.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'y.Length')
                              Instance Receiver: 
                                IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'y.Length')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_QueryExpressionGroupByClause()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Class C
    Public Sub Method(x As String())
        Dim c = From y In x Group By w = x, z = y Into Count()'BIND:"From y In x Group By w = x, z = y Into Count()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryExpression (OperationKind.TranslatedQueryExpression, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) (Syntax: 'From y In x ... nto Count()')
  Expression: 
    IInvocationExpression ( Function System.Collections.Generic.IEnumerable(Of System.String).GroupBy(Of <anonymous type: Key w As System.String(), Key z As System.String>, <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)(keySelector As System.Func(Of System.String, <anonymous type: Key w As System.String(), Key z As System.String>), resultSelector As System.Func(Of <anonymous type: Key w As System.String(), Key z As System.String>, System.Collections.Generic.IEnumerable(Of System.String), <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
      Instance Receiver: 
        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable(Of System.String)) (Syntax: 'y In x')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String()) (Syntax: 'x')
      Arguments(2):
          IArgument (ArgumentKind.DefaultValue, Matching Parameter: keySelector) (OperationKind.Argument, IsImplicit) (Syntax: 'x')
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String, <anonymous type: Key w As System.String(), Key z As System.String>), IsImplicit) (Syntax: 'x')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IAnonymousFunctionExpression (Symbol: Function (y As System.String) As <anonymous type: Key w As System.String(), Key z As System.String>) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'x')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'x')
                    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x')
                      ReturnedValue: 
                        IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                          Initializers(2):
                              IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String()) (Syntax: 'x')
                              IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgument (ArgumentKind.DefaultValue, Matching Parameter: resultSelector) (OperationKind.Argument, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of <anonymous type: Key w As System.String(), Key z As System.String>, System.Collections.Generic.IEnumerable(Of System.String), <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IAnonymousFunctionExpression (Symbol: Function ($VB$It As <anonymous type: Key w As System.String(), Key z As System.String>, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.String)) As <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>) (OperationKind.AnonymousFunctionExpression, Type: null, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Group By w  ... nto Count()')
                    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Group By w  ... nto Count()')
                      ReturnedValue: 
                        IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                          Initializers(3):
                              IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.w As System.String() (OperationKind.PropertyReferenceExpression, Type: System.String()) (Syntax: 'Group By w  ... nto Count()')
                                Instance Receiver: 
                                  IParameterReferenceExpression: $VB$It (OperationKind.ParameterReferenceExpression, Type: <anonymous type: Key w As System.String(), Key z As System.String>) (Syntax: 'Group By w  ... nto Count()')
                              IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.z As System.String (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Group By w  ... nto Count()')
                                Instance Receiver: 
                                  IParameterReferenceExpression: $VB$It (OperationKind.ParameterReferenceExpression, Type: <anonymous type: Key w As System.String(), Key z As System.String>) (Syntax: 'Group By w  ... nto Count()')
                              IInvocationExpression ( Function System.Collections.Generic.IEnumerable(Of System.String).Count() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                Instance Receiver: 
                                  IParameterReferenceExpression: $VB$ItAnonymous (OperationKind.ParameterReferenceExpression, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Arguments(0)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_ObjectAndCollectionInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic

Friend Class [Class]
    Public Property X As Integer
    Public Property Y As Integer()
    Public Property Z As Dictionary(Of Integer, Integer)
    Public Property C As [Class]

    Public Sub M(x As Integer, y As Integer, z As Integer)
        Dim c = New [Class]() With {'BIND:"New [Class]() With {"'BIND:"New [Class]() With {'BIND:"New [Class]() With {""
            .X = x,
            .Y = {x, y, 3},
            .Z = New Dictionary(Of Integer, Integer) From {{x, y}},
            .C = New [Class]() With {.X = z}
        }
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationExpression (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreationExpression, Type: [Class]) (Syntax: 'New [Class] ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: [Class]) (Syntax: 'With {'BIND ... }')
      Initializers(4):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: '.X = x')
            Left: 
              IPropertyReferenceExpression: Property [Class].X As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: '.Y = {x, y, 3}')
            Left: 
              IPropertyReferenceExpression: Property [Class].Y As System.Int32() (OperationKind.PropertyReferenceExpression, Type: System.Int32()) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IArrayCreationExpression (OperationKind.ArrayCreationExpression, Type: System.Int32()) (Syntax: '{x, y, 3}')
                Dimension Sizes(1):
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{x, y, 3}')
                Initializer: 
                  IArrayInitializer (3 elements) (OperationKind.ArrayInitializer, IsImplicit) (Syntax: '{x, y, 3}')
                    Element Values(3):
                        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                        IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: '.Z = New Di ... om {{x, y}}')
            Left: 
              IPropertyReferenceExpression: Property [Class].Z As System.Collections.Generic.Dictionary(Of System.Int32, System.Int32) (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationExpression (Constructor: Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'New Diction ... om {{x, y}}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'From {{x, y}}')
                    Initializers(1):
                        ICollectionElementInitializerExpression (AddMethod: Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32).Add(key As System.Int32, value As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializerExpression, Type: System.Void, IsImplicit) (Syntax: '{x, y}')
                          Arguments(2):
                              IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                              IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: '.C = New [C ... th {.X = z}')
            Left: 
              IPropertyReferenceExpression: Property [Class].C As [Class] (OperationKind.PropertyReferenceExpression, Type: [Class]) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationExpression (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreationExpression, Type: [Class]) (Syntax: 'New [Class] ... th {.X = z}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: [Class]) (Syntax: 'With {.X = z}')
                    Initializers(1):
                        ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: '.X = z')
                          Left: 
                            IPropertyReferenceExpression: Property [Class].X As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'X')
                              Instance Receiver: 
                                IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... th {.X = z}')
                          Right: 
                            IParameterReferenceExpression: z (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'z')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_DelegateCreationExpressionWithLambdaArgument()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class Class1
    Delegate Sub DelegateType()
    Public Sub M(x As Object, y As EventArgs)
        Dim eventHandler As New EventHandler(Function() x)'BIND:"New EventHandler(Function() x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILocalInitializer (OperationKind.LocalInitializer) (Syntax: 'New EventHa ... nction() x)')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.EventHandler) (Syntax: 'New EventHa ... nction() x)')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IAnonymousFunctionExpression (Symbol: Function () As System.Object) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: 'Function() x')
        IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsImplicit) (Syntax: 'Function() x')
          Locals: Local_1: <anonymous local> As System.Object
          IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x')
            ReturnedValue: 
              IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'x')
          ILabeledStatement (Label: exit) (OperationKind.LabeledStatement, IsImplicit) (Syntax: 'Function() x')
            Statement: 
              null
          IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'Function() x')
            ReturnedValue: 
              ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object, IsImplicit) (Syntax: 'Function() x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_DelegateCreationExpressionWithMethodArgument()
            Dim source = <![CDATA[
Imports System

Class Class1
    Public Sub M(x As Object, y As EventArgs)
        Dim eventHandler As New EventHandler(AddressOf Me.M)'BIND:"New EventHandler(AddressOf Me.M)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILocalInitializer (OperationKind.LocalInitializer) (Syntax: 'New EventHa ... essOf Me.M)')
  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.EventHandler) (Syntax: 'New EventHa ... essOf Me.M)')
    Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Operand: 
      IOperation:  (OperationKind.None) (Syntax: 'AddressOf Me.M')
        Children(1):
            IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Class1) (Syntax: 'Me')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_DelegateCreationExpressionWithInvalidArgument()
            Dim source = <![CDATA[
Option Strict Off
Imports System

Class Class1
    Delegate Sub DelegateType()
    Public Sub M(x As Object, y As EventArgs)
        Dim eventHandler As New EventHandler(x)'BIND:"New EventHandler(x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
ILocalInitializer (OperationKind.LocalInitializer, IsInvalid) (Syntax: 'New EventHandler(x)')
  IInvalidExpression (OperationKind.InvalidExpression, Type: System.EventHandler, IsInvalid) (Syntax: 'New EventHandler(x)')
    Children(1):
        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32008: Delegate 'EventHandler' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
        Dim eventHandler As New EventHandler(x)'BIND:"New EventHandler(x)"
                                            ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_NameOfExpression()
            Dim source = <![CDATA[
Class Class1
    Public Function M(x As Integer) As String
        Return NameOf(x)'BIND:"NameOf(x)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
INameOfExpression (OperationKind.NameOfExpression, Type: System.String, Constant: "x") (Syntax: 'NameOf(x)')
  IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of NameOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_NameOfExpression_ErrorCase()
            Dim source = <![CDATA[
Class Class1
    Public Function M(x As Integer, y As Integer) As String
        Return NameOf(x + y)'BIND:"NameOf(x + y)"
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
INameOfExpression (OperationKind.NameOfExpression, Type: System.String, Constant: null, IsInvalid) (Syntax: 'NameOf(x + y)')
  IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'x + y')
    Left: 
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
    Right: 
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC37244: This expression does not have a name.
        Return NameOf(x + y)'BIND:"NameOf(x + y)"
                      ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of NameOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_LateBoundIndexerAccess()
            Dim source = <![CDATA[
Option Strict Off

Class Class1
    Public Sub M(d As Object, x As Integer)
        Dim y = d(x)'BIND:"d(x)"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'd(x)')
  Expression: 
    IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
  Arguments(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'x')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_LateBoundMemberAccess()
            Dim source = <![CDATA[
Option Strict Off

Class Class1
    Public Sub M(x As Object, y As Integer)
        Dim z = x.M(y)'BIND:"x.M(y)"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'x.M(y)')
  Expression: 
    IDynamicMemberReferenceExpression (Member Name: "M", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'x.M')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'x')
  Arguments(1):
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_LateBoundInvocation()
            Dim source = <![CDATA[
Option Strict Off

Class Class1
    Public Sub M(x As Object, y As Integer)
        Dim z = x(y)'BIND:"x(y)"
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: System.Object) (Syntax: 'x(y)')
  Expression: 
    IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'x')
  Arguments(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'y')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_InterpolatedStringExpression()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As String, y As Integer)
        Console.WriteLine($"String {x,20} and {y:D3} and constant {1}")'BIND:"$"String {x,20} and {y:D3} and constant {1}""
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "String ") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,20}')
        Expression: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'x')
        Alignment: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and ") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{y:D3}')
        Expression: 
          IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "D3") (Syntax: ':D3')
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " and constant ") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: 
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of InterpolatedStringExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_MidAssignmentStatement()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(str As String, start As Integer, length As Integer)
        Mid(str, start, length) = str'BIND:"Mid(str, start, length) = str"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Mid(str, st ... ngth) = str')
  Expression: 
    ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Void) (Syntax: 'Mid(str, st ... ngth) = str')
      Left: 
        IParameterReferenceExpression: str (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'str')
      Right: 
        IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'Mid(str, st ... ngth) = str')
          Children(4):
              IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.String) (Syntax: 'Mid(str, start, length)')
                Operand: 
                  IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'str')
              IParameterReferenceExpression: start (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'start')
              IParameterReferenceExpression: length (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'length')
              IParameterReferenceExpression: str (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'str')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AssignmentStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_MisplacedCaseStatement()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        Case x'BIND:"Case x"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Case x')
  Children(1):
      ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause, IsInvalid) (Syntax: 'x')
        Value: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30072: 'Case' can only appear inside a 'Select Case' statement.
        Case x'BIND:"Case x"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of CaseStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_RedimStatement()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)
        Dim intArray(10, 10, 10) As Integer
        ReDim intArray(x, x, x)'BIND:"ReDim intArray(x, x, x)"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None) (Syntax: 'ReDim intArray(x, x, x)')
  Children(1):
      IOperation:  (OperationKind.None) (Syntax: 'intArray(x, x, x)')
        Children(4):
            ILocalReferenceExpression: intArray (OperationKind.LocalReferenceExpression, Type: System.Int32(,,)) (Syntax: 'intArray')
            IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
            IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
            IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ReDimStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_EraseStatement()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer())
        Erase x'BIND:"Erase x"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None) (Syntax: 'Erase x')
  Children(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32(), IsImplicit) (Syntax: 'x')
        Left: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32()) (Syntax: 'x')
        Right: 
          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'x')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, IsImplicit) (Syntax: 'Erase x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of EraseStatementSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/19024"), WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_UnstructuredExceptionHandlingStatement()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(x As Integer)'BIND:"Public Sub M(x As Integer)"
        Resume Next
        Console.Write(x)
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'Public Sub  ... End Sub')
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_LateAddressOfOperator()
            Dim source = <![CDATA[
Option Strict Off

Class Class1
    Public Sub M(x As Object)
        Dim y = AddressOf x.Method'BIND:"AddressOf x.Method"
    End Sub
    Public Sub M2(x As Boolean?)

    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None) (Syntax: 'AddressOf x.Method')
  Children(1):
      IDynamicMemberReferenceExpression (Member Name: "Method", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: System.Object) (Syntax: 'x.Method')
        Type Arguments(0)
        Instance Receiver: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of UnaryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_NullableIsTrueOperator()
            Dim source = <![CDATA[
Option Strict Off

Class Class1
    Public Sub M(x As Boolean?)
        If x Then'BIND:"If x Then"
        End If
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement) (Syntax: 'If x Then'B ... End If')
  Condition: 
    IOperation:  (OperationKind.None, IsImplicit) (Syntax: 'x')
      Children(1):
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Nullable(Of System.Boolean)) (Syntax: 'x')
  IfTrue: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'If x Then'B ... End If')
  IfFalse: 
    null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")>
        Public Sub ParameterReference_NoPiaObjectCreation()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>
<ComImport()>
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58277")>
<CoClass(GetType(C))>
Public Interface I
    Property P As Integer
End Interface
<Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58278")>
Public Class C
    Public Sub New(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Structure S
    Function F(x as Object) As I
        Return New I(x)'BIND:"New I(x)"
    End Function
End Structure
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0)
            compilation0.AssertTheseDiagnostics()

            ' No errors for /r:_.dll
            Dim compilation1 = CreateCompilationWithReferences(
                sources1,
                references:={MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes:=True)})

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'New I(x)')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Return New I(x)'BIND:"New I(x)"
               ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(compilation1, "a.vb", expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
