' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
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
ITupleOperation (OperationKind.Tuple, Type: (x As System.Int32, System.Int32)) (Syntax: '(x, x + y)')
  NaturalType: (x As System.Int32, System.Int32)
  Elements(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
        Left: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Right: 
          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key Amount As System.Int32, Key Message As System.String>) (Syntax: 'New With {' ... }')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Key .Amount = x')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key Amount As System.Int32, Key Message As System.String>.Amount As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Amount')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key Amount As System.Int32, Key Message As System.String>, IsImplicit) (Syntax: 'New With {' ... }')
        Right: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'Key .Messag ... "Hello" + y')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key Amount As System.Int32, Key Message As System.String>.Message As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Message')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key Amount As System.Int32, Key Message As System.String>, IsImplicit) (Syntax: 'New With {' ... }')
        Right: 
          IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"Hello" + y')
            Left: 
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello") (Syntax: '"Hello"')
            Right: 
              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
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
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of System.String)) (Syntax: 'From cust I ... t cust.Name')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of Customer).Select(Of System.String)(selector As System.Func(Of Customer, System.String)) As System.Collections.Generic.IEnumerable(Of System.String)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'Select cust.Name')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of Customer), IsImplicit) (Syntax: 'cust In customers')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: customers (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of Customer)) (Syntax: 'customers')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'cust.Name')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of Customer, System.String), IsImplicit) (Syntax: 'cust.Name')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (cust As Customer) As System.String) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'cust.Name')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'cust.Name')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'cust.Name')
                      ReturnedValue: 
                        IPropertyReferenceOperation: Property Customer.Name As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'cust.Name')
                          Instance Receiver: 
                            IParameterReferenceOperation: cust (OperationKind.ParameterReference, Type: Customer) (Syntax: 'cust')
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
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Int32) (Syntax: 'Aggregate y ... nto Count()')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'y In New Integer() {x}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {x}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {x}')
              Initializer: 
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{x}')
                  Element Values(1):
                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Linq.IOrderedEnumerable(Of System.String)) (Syntax: 'From y In x ... By y.Length')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.String).OrderBy(Of System.Int32)(keySelector As System.Func(Of System.String, System.Int32)) As System.Linq.IOrderedEnumerable(Of System.String)) (OperationKind.Invocation, Type: System.Linq.IOrderedEnumerable(Of System.String), IsImplicit) (Syntax: 'y.Length')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'y In x')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'x')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y.Length')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String, System.Int32), IsImplicit) (Syntax: 'y.Length')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (y As System.String) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'y.Length')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'y.Length')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'y.Length')
                      ReturnedValue: 
                        IPropertyReferenceOperation: ReadOnly Property System.String.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y.Length')
                          Instance Receiver: 
                            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
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
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) (Syntax: 'From y In x ... nto Count()')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.String).GroupBy(Of <anonymous type: Key w As System.String(), Key z As System.String>, <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)(keySelector As System.Func(Of System.String, <anonymous type: Key w As System.String(), Key z As System.String>), resultSelector As System.Func(Of <anonymous type: Key w As System.String(), Key z As System.String>, System.Collections.Generic.IEnumerable(Of System.String), <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'y In x')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'x')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String, <anonymous type: Key w As System.String(), Key z As System.String>), IsImplicit) (Syntax: 'x')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (y As System.String) As <anonymous type: Key w As System.String(), Key z As System.String>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String(), IsImplicit) (Syntax: 'w = x')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.w As System.String() (OperationKind.PropertyReference, Type: System.String(), IsImplicit) (Syntax: 'x')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Right: 
                                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'x')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'z = y')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.z As System.String (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'y')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Right: 
                                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String) (Syntax: 'y')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key w As System.String(), Key z As System.String>, System.Collections.Generic.IEnumerable(Of System.String), <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key w As System.String(), Key z As System.String>, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.String)) As <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String(), IsImplicit) (Syntax: 'w =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>.w As System.String() (OperationKind.PropertyReference, Type: System.String(), IsImplicit) (Syntax: 'w')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.w As System.String() (OperationKind.PropertyReference, Type: System.String(), IsImplicit) (Syntax: 'w')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'z =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>.z As System.String (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'z')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String>.z As System.String (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'z')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key w As System.String(), Key z As System.String>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>.Count As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key w As System.String(), Key z As System.String, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By w  ... nto Count()')
                                Right: 
                                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.String).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'Group By w  ... nto Count()')
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
IObjectCreationOperation (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreation, Type: [Class]) (Syntax: 'New [Class] ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: [Class]) (Syntax: 'With {'BIND ... }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.X = x')
            Left: 
              IPropertyReferenceOperation: Property [Class].X As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Y = {x, y, 3}')
            Left: 
              IPropertyReferenceOperation: Property [Class].Y As System.Int32() (OperationKind.PropertyReference, Type: System.Int32()) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: '{x, y, 3}')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{x, y, 3}')
                Initializer: 
                  IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{x, y, 3}')
                    Element Values(3):
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Z = New Di ... om {{x, y}}')
            Left: 
              IPropertyReferenceOperation: Property [Class].Z As System.Collections.Generic.Dictionary(Of System.Int32, System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationOperation (Constructor: Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'New Diction ... om {{x, y}}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'From {{x, y}}')
                    Initializers(1):
                        IInvocationOperation ( Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32).Add(key As System.Int32, value As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{x, y}')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'New Diction ... om {{x, y}}')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.C = New [C ... th {.X = z}')
            Left: 
              IPropertyReferenceOperation: Property [Class].C As [Class] (OperationKind.PropertyReference, Type: [Class]) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationOperation (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreation, Type: [Class]) (Syntax: 'New [Class] ... th {.X = z}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: [Class]) (Syntax: 'With {.X = z}')
                    Initializers(1):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.X = z')
                          Left: 
                            IPropertyReferenceOperation: Property [Class].X As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... th {.X = z}')
                          Right: 
                            IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler) (Syntax: 'New EventHa ... nction() x)')
  Target: 
    IAnonymousFunctionOperation (Symbol: Function () As System.Object) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'Function() x')
      IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Function() x')
        Locals: Local_1: <anonymous local> As System.Object
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
          ReturnedValue: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
        ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'Function() x')
          Statement: 
            null
        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Function() x')
          ReturnedValue: 
            ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'Function() x')
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
IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.EventHandler) (Syntax: 'New EventHa ... essOf Me.M)')
  Target: 
    IMethodReferenceOperation: Sub Class1.M(x As System.Object, y As System.EventArgs) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf Me.M')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class1) (Syntax: 'Me')
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
IInvalidOperation (OperationKind.Invalid, Type: System.EventHandler, IsInvalid) (Syntax: 'New EventHandler(x)')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'x')
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
INameOfOperation (OperationKind.NameOf, Type: System.String, Constant: "x") (Syntax: 'NameOf(x)')
  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
INameOfOperation (OperationKind.NameOf, Type: System.String, Constant: null, IsInvalid) (Syntax: 'NameOf(x + y)')
  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsInvalid) (Syntax: 'x + y')
    Left: 
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
    Right: 
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
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
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'd(x)')
  Expression: 
    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
  Arguments(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'x')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'x.M(y)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "M", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'x.M')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
  Arguments(1):
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: 'x(y)')
  Expression: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
  Arguments(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$"String {x ... nstant {1}"')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "String ", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20}')
        Expression: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and ", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y:D3}')
        Expression: 
          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "D3") (Syntax: ':D3')
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " and constant ", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Mid(str, st ... ngth) = str')
  Expression: 
    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, st ... ngth) = str')
      Children(2):
          IParameterReferenceOperation: str (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str')
          IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'Mid(str, st ... ngth) = str')
            Children(4):
                IParenthesizedOperation (OperationKind.Parenthesized, Type: System.String) (Syntax: 'Mid(str, start, length)')
                  Operand: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'str')
                IParameterReferenceOperation: start (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'start')
                IParameterReferenceOperation: length (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'length')
                IParameterReferenceOperation: str (OperationKind.ParameterReference, Type: System.String) (Syntax: 'str')
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
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Case x')
  Children(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
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
IReDimOperation (OperationKind.ReDim, Type: null) (Syntax: 'ReDim intArray(x, x, x)')
  Clauses(1):
      IReDimClauseOperation (OperationKind.ReDimClause, Type: null) (Syntax: 'intArray(x, x, x)')
        Operand: 
          ILocalReferenceOperation: intArray (OperationKind.LocalReference, Type: System.Int32(,,)) (Syntax: 'intArray')
        DimensionSizes(3):
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'x')
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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'Erase x')
  Children(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32(), IsImplicit) (Syntax: 'x')
        Left: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32(), Constant: null, IsImplicit) (Syntax: 'x')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'x')
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
  IReturnStatement (OperationKind.ReturnStatement, IsImplicit) (Syntax: 'End Sub')
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
IOperation:  (OperationKind.None, Type: null) (Syntax: 'AddressOf x.Method')
  Children(1):
      IDynamicMemberReferenceOperation (Member Name: "Method", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: 'x.Method')
        Type Arguments(0)
        Instance Receiver: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
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
IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If x Then'B ... End If')
  Condition: 
    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
      Instance Receiver: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of System.Boolean)) (Syntax: 'x')
      Arguments(0)
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If x Then'B ... End If')
  WhenFalse: 
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
            Dim compilation0 = CreateCompilationWithMscorlib40(sources0)
            compilation0.AssertTheseDiagnostics()

            ' No errors for /r:_.dll
            Dim compilation1 = CreateEmptyCompilationWithReferences(
                sources1,
                references:={MscorlibRef, SystemRef, compilation0.EmitToImageReference(embedInteropTypes:=True)})

            Dim expectedOperationTree = <![CDATA[
INoPiaObjectCreationOperation (OperationKind.None, Type: I, IsInvalid) (Syntax: 'New I(x)')
  Initializer: 
    null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Return New I(x)'BIND:"New I(x)"
               ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(compilation1, "a.vb", expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub LocalReference_ParameterReference_Flow()
            Dim source = <![CDATA[
Imports System

Friend Class [Class]
    Public Sub M(p As Integer)'BIND:"Public Sub M(p As Integer)"
        Dim l = p
        p = l
    End Sub
End Class]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [l As System.Int32]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'l = p')
              Left: 
                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'l')
              Right: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = l')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'p = l')
                  Left: 
                    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
                  Right: 
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'l')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
