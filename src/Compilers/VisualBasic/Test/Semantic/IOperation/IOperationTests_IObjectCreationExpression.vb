' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")>
        Public Sub ObjectCreationWithMemberInitializers()
            Dim source = <![CDATA[
Structure B
    Public Field As Boolean
End Structure

Class F
    Public Field As Integer
    Public Property Property1() As String
    Public Property Property2() As B
End Class

Class C
    Public Sub M1()'BIND:"Public Sub M1()"
        Dim x1 = New F()
        Dim x2 = New F() With {.Field = 2}
        Dim x3 = New F() With {.Property1 = ""}
        Dim x4 = New F() With {.Property1 = "", .Field = 2}
        Dim x5 = New F() With {.Property2 = New B() With {.Field = True}}

        Dim e1 = New F() With {.Property2 = 1}
        Dim e2 = New F() From {""}
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (9 statements, 7 locals) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: 'Public Sub  ... End Sub')
  Locals: Local_1: x1 As F
    Local_2: x2 As F
    Local_3: x3 As F
    Local_4: x4 As F
    Local_5: x5 As F
    Local_6: e1 As F
    Local_7: e2 As F
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'Dim x1 = New F()')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x1')
      Variables: Local_1: x1 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F()')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'New F()')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'Dim x2 = Ne ... .Field = 2}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x2')
      Variables: Local_1: x2 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... .Field = 2}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'New F() Wit ... .Field = 2}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: 'With {.Field = 2}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: '.Field = 2')
                      Left: 
                        IFieldReferenceOperation: F.Field As System.Int32 (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'Dim x3 = Ne ... erty1 = ""}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x3')
      Variables: Local_1: x3 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... erty1 = ""}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'New F() Wit ... erty1 = ""}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: 'With {.Property1 = ""}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.Property1 = ""')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property1 As System.String (OperationKind.PropertyReference, IsExpression, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsImplicit) (Syntax: 'New F() Wit ... erty1 = ""}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: "") (Syntax: '""')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'Dim x4 = Ne ... .Field = 2}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x4')
      Variables: Local_1: x4 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... .Field = 2}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'New F() Wit ... .Field = 2}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: 'With {.Prop ... .Field = 2}')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.Property1 = ""')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property1 As System.String (OperationKind.PropertyReference, IsExpression, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: "") (Syntax: '""')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: '.Field = 2')
                      Left: 
                        IFieldReferenceOperation: F.Field As System.Int32 (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'Dim x5 = Ne ... ld = True}}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x5')
      Variables: Local_1: x5 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... ld = True}}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F) (Syntax: 'New F() Wit ... ld = True}}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F) (Syntax: 'With {.Prop ... ld = True}}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.Property2  ... eld = True}')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property2 As B (OperationKind.PropertyReference, IsExpression, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsImplicit) (Syntax: 'New F() Wit ... ld = True}}')
                      Right: 
                        IObjectCreationOperation (Constructor: Sub B..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: B) (Syntax: 'New B() Wit ... eld = True}')
                          Arguments(0)
                          Initializer: 
                            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: B) (Syntax: 'With {.Field = True}')
                              Initializers(1):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: '.Field = True')
                                    Left: 
                                      IFieldReferenceOperation: B.Field As System.Boolean (OperationKind.FieldReference, IsExpression, Type: System.Boolean) (Syntax: 'Field')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: B, IsImplicit) (Syntax: 'New B() Wit ... eld = True}')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'True')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null, IsInvalid) (Syntax: 'Dim e1 = Ne ... perty2 = 1}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'e1')
      Variables: Local_1: e1 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New F() W ... perty2 = 1}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F, IsInvalid) (Syntax: 'New F() Wit ... perty2 = 1}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F, IsInvalid) (Syntax: 'With {.Property2 = 1}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void, IsInvalid) (Syntax: '.Property2 = 1')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property2 As B (OperationKind.PropertyReference, IsExpression, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: F, IsInvalid, IsImplicit) (Syntax: 'New F() Wit ... perty2 = 1}')
                      Right: 
                        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null, IsInvalid) (Syntax: 'Dim e2 = Ne ... ) From {""}')
    IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'e2')
      Variables: Local_1: e2 As F
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New F() From {""}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: F, IsInvalid) (Syntax: 'New F() From {""}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: F, IsInvalid) (Syntax: 'From {""}')
                Initializers(1):
                    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: '""')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, IsStatement, Type: null) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'Integer' cannot be converted to 'B'.
        Dim e1 = New F() With {.Property2 = 1}
                                            ~
BC36718: Cannot initialize the type 'F' with a collection initializer because it is not a collection type.
        Dim e2 = New F() From {""}
                         ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")>
        Public Sub ObjectCreationWithCollectionInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic

Class C
    Private ReadOnly field As Integer

    Public Sub M1(x As Integer)
        Dim y As Integer = 0
        Dim x1 = New List(Of Integer) From {x, y, field}'BIND:"New List(Of Integer) From {x, y, field}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Int32)..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'New List(Of ... , y, field}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'From {x, y, field}')
      Initializers(3):
          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'x')
            Arguments(1):
                IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'y')
            Arguments(1):
                ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'field')
            Arguments(1):
                IFieldReferenceOperation: C.field As System.Int32 (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                  Instance Receiver: 
                    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C, IsImplicit) (Syntax: 'field')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")>
        Public Sub ObjectCreationWithNestedCollectionInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Imports System.Linq

Class C
    Private ReadOnly field As Integer

    Public Sub M1(x As Integer)
        Dim y As Integer = 0
        Dim x1 = New List(Of List(Of Integer)) From {{x, y}.ToList, New List(Of Integer) From {field}}'BIND:"New List(Of List(Of Integer)) From {{x, y}.ToList, New List(Of Integer) From {field}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))) (Syntax: 'New List(Of ... om {field}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))) (Syntax: 'From {{x, y ... om {field}}')
      Initializers(2):
          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)).Add(item As System.Collections.Generic.List(Of System.Int32))) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: '{x, y}.ToList')
            Arguments(1):
                IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).ToList() As System.Collections.Generic.List(Of System.Int32)) (OperationKind.Invocation, IsExpression, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: '{x, y}.ToList')
                  Instance Receiver: 
                    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: '{x, y}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IArrayCreationOperation (OperationKind.ArrayCreation, IsExpression, Type: System.Int32()) (Syntax: '{x, y}')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{x, y}')
                          Initializer: 
                            IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{x, y}')
                              Element Values(2):
                                  IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                                  ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
                  Arguments(0)
          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)).Add(item As System.Collections.Generic.List(Of System.Int32))) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'New List(Of ... rom {field}')
            Arguments(1):
                IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Int32)..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'New List(Of ... rom {field}')
                  Arguments(0)
                  Initializer: 
                    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'From {field}')
                      Initializers(1):
                          ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: 'field')
                            Arguments(1):
                                IFieldReferenceOperation: C.field As System.Int32 (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                                  Instance Receiver: 
                                    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: C, IsImplicit) (Syntax: 'field')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17588, "https://github.com/dotnet/roslyn/issues/17588")>
        Public Sub ObjectCreationWithMemberAndCollectionInitializers()
            Dim source = <![CDATA[
Imports System.Collections.Generic

Friend Class [Class]
    Public Property X As Integer
    Public Property Y As Integer()
    Public Property Z As Dictionary(Of Integer, Integer)
    Public Property C As [Class]

    Private ReadOnly field As Integer

    Public Sub M(x As Integer)
        Dim y As Integer = 0
        Dim c = New [Class]() With {'BIND:"New [Class]() With {"
            .X = x,
            .Y = {x, y, 3},
            .Z = New Dictionary(Of Integer, Integer) From {{x, y}},
            .C = New [Class]() With {.X = field}
        }
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: [Class]) (Syntax: 'New [Class] ... }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: [Class]) (Syntax: 'With {'BIND ... }')
      Initializers(4):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.X = x')
            Left: 
              IPropertyReferenceOperation: Property [Class].X As System.Int32 (OperationKind.PropertyReference, IsExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.Y = {x, y, 3}')
            Left: 
              IPropertyReferenceOperation: Property [Class].Y As System.Int32() (OperationKind.PropertyReference, IsExpression, Type: System.Int32()) (Syntax: 'Y')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IArrayCreationOperation (OperationKind.ArrayCreation, IsExpression, Type: System.Int32()) (Syntax: '{x, y, 3}')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: '{x, y, 3}')
                Initializer: 
                  IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{x, y, 3}')
                    Element Values(3):
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                        ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.Z = New Di ... om {{x, y}}')
            Left: 
              IPropertyReferenceOperation: Property [Class].Z As System.Collections.Generic.Dictionary(Of System.Int32, System.Int32) (OperationKind.PropertyReference, IsExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'Z')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationOperation (Constructor: Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'New Diction ... om {{x, y}}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: System.Collections.Generic.Dictionary(Of System.Int32, System.Int32)) (Syntax: 'From {{x, y}}')
                    Initializers(1):
                        ICollectionElementInitializerOperation (AddMethod: Sub System.Collections.Generic.Dictionary(Of System.Int32, System.Int32).Add(key As System.Int32, value As System.Int32)) (IsDynamic: False) (OperationKind.CollectionElementInitializer, IsExpression, Type: System.Void, IsImplicit) (Syntax: '{x, y}')
                          Arguments(2):
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                              ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'y')
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.C = New [C ... .X = field}')
            Left: 
              IPropertyReferenceOperation: Property [Class].C As [Class] (OperationKind.PropertyReference, IsExpression, Type: [Class]) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationOperation (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: [Class]) (Syntax: 'New [Class] ... .X = field}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, IsExpression, Type: [Class]) (Syntax: 'With {.X = field}')
                    Initializers(1):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Void) (Syntax: '.X = field')
                          Left: 
                            IPropertyReferenceOperation: Property [Class].X As System.Int32 (OperationKind.PropertyReference, IsExpression, Type: System.Int32) (Syntax: 'X')
                              Instance Receiver: 
                                IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... .X = field}')
                          Right: 
                            IFieldReferenceOperation: [Class].field As System.Int32 (OperationKind.FieldReference, IsExpression, Type: System.Int32) (Syntax: 'field')
                              Instance Receiver: 
                                IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: [Class], IsImplicit) (Syntax: 'field')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
