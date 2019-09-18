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
IBlockOperation (9 statements, 7 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Public Sub  ... End Sub')
  Locals: Local_1: x1 As F
    Local_2: x2 As F
    Local_3: x3 As F
    Local_4: x4 As F
    Local_5: x5 As F
    Local_6: e1 As F
    Local_7: e2 As F
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x1 = New F()')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x1 = New F()')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x1 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x1')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F()')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'New F()')
            Arguments(0)
            Initializer: 
              null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x2 = Ne ... .Field = 2}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x2 = New F( ... .Field = 2}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x2 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x2')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... .Field = 2}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'New F() Wit ... .Field = 2}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: 'With {.Field = 2}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = 2')
                      Left: 
                        IFieldReferenceOperation: F.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x3 = Ne ... erty1 = ""}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x3 = New F( ... erty1 = ""}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x3 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x3')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... erty1 = ""}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'New F() Wit ... erty1 = ""}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: 'With {.Property1 = ""}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Property1 = ""')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property1 As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'New F() Wit ... erty1 = ""}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "") (Syntax: '""')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x4 = Ne ... .Field = 2}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x4 = New F( ... .Field = 2}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x4 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x4')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... .Field = 2}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'New F() Wit ... .Field = 2}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: 'With {.Prop ... .Field = 2}')
                Initializers(2):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Property1 = ""')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property1 As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Property1')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "") (Syntax: '""')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.Field = 2')
                      Left: 
                        IFieldReferenceOperation: F.Field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'Field')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'New F() Wit ... .Field = 2}')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim x5 = Ne ... ld = True}}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'x5 = New F( ... ld = True}}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: x5 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x5')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New F() W ... ld = True}}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'New F() Wit ... ld = True}}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: 'With {.Prop ... ld = True}}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.Property2  ... eld = True}')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property2 As B (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'New F() Wit ... ld = True}}')
                      Right: 
                        IObjectCreationOperation (Constructor: Sub B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'New B() Wit ... eld = True}')
                          Arguments(0)
                          Initializer: 
                            IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B) (Syntax: 'With {.Field = True}')
                              Initializers(1):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: '.Field = True')
                                    Left: 
                                      IFieldReferenceOperation: B.Field As System.Boolean (OperationKind.FieldReference, Type: System.Boolean) (Syntax: 'Field')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'New B() Wit ... eld = True}')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim e1 = Ne ... perty2 = 1}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'e1 = New F( ... perty2 = 1}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: e1 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e1')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New F() W ... perty2 = 1}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'New F() Wit ... perty2 = 1}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: 'With {.Property2 = 1}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsInvalid) (Syntax: '.Property2 = 1')
                      Left: 
                        IPropertyReferenceOperation: Property F.Property2 As B (OperationKind.PropertyReference, Type: B) (Syntax: 'Property2')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: F, IsInvalid, IsImplicit) (Syntax: 'New F() Wit ... perty2 = 1}')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B, IsInvalid, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim e2 = Ne ... ) From {""}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'e2 = New F() From {""}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: e2 As F) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'e2')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New F() From {""}')
          IObjectCreationOperation (Constructor: Sub F..ctor()) (OperationKind.ObjectCreation, Type: F, IsInvalid) (Syntax: 'New F() From {""}')
            Arguments(0)
            Initializer: 
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F, IsInvalid) (Syntax: 'From {""}')
                Initializers(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '""')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsInvalid) (Syntax: '""')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
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
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'New List(Of ... , y, field}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'From {x, y, field}')
      Initializers(3):
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'New List(Of ... , y, field}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                  IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'New List(Of ... , y, field}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'New List(Of ... , y, field}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                  IFieldReferenceOperation: C.field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))) (Syntax: 'New List(Of ... om {field}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32))) (Syntax: 'From {{x, y ... om {field}}')
      Initializers(2):
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)).Add(item As System.Collections.Generic.List(Of System.Int32))) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{x, y}.ToList')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)), IsImplicit) (Syntax: 'New List(Of ... om {field}}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{x, y}.ToList')
                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).ToList() As System.Collections.Generic.List(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: '{x, y}.ToList')
                    Instance Receiver: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: '{x, y}')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: '{x, y}')
                            Dimension Sizes(1):
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '{x, y}')
                            Initializer: 
                              IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{x, y}')
                                Element Values(2):
                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                                    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                    Arguments(0)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)).Add(item As System.Collections.Generic.List(Of System.Int32))) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'New List(Of ... rom {field}')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Collections.Generic.List(Of System.Int32)), IsImplicit) (Syntax: 'New List(Of ... om {field}}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New List(Of ... rom {field}')
                  IObjectCreationOperation (Constructor: Sub System.Collections.Generic.List(Of System.Int32)..ctor()) (OperationKind.ObjectCreation, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'New List(Of ... rom {field}')
                    Arguments(0)
                    Initializer: 
                      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'From {field}')
                        Initializers(1):
                            IInvocationOperation ( Sub System.Collections.Generic.List(Of System.Int32).Add(item As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'field')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'New List(Of ... rom {field}')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'field')
                                    IFieldReferenceOperation: C.field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                                      Instance Receiver: 
                                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
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
                                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.C = New [C ... .X = field}')
            Left: 
              IPropertyReferenceOperation: Property [Class].C As [Class] (OperationKind.PropertyReference, Type: [Class]) (Syntax: 'C')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... }')
            Right: 
              IObjectCreationOperation (Constructor: Sub [Class]..ctor()) (OperationKind.ObjectCreation, Type: [Class]) (Syntax: 'New [Class] ... .X = field}')
                Arguments(0)
                Initializer: 
                  IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: [Class]) (Syntax: 'With {.X = field}')
                    Initializers(1):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.X = field')
                          Left: 
                            IPropertyReferenceOperation: Property [Class].X As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'New [Class] ... .X = field}')
                          Right: 
                            IFieldReferenceOperation: [Class].field As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'field')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: [Class], IsImplicit) (Syntax: 'field')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")>
        Public Sub ObjectCreationWithInvalidInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub M1()
        Dim x1 = New C With {.MissingField = 1}'BIND:"New C With {.MissingField = 1}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C With  ... gField = 1}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: 'With {.MissingField = 1}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.MissingField = 1')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.MissingField = 1')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'New C With  ... gField = 1}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'MissingField' is not a member of 'C'.
        Dim x1 = New C With {.MissingField = 1}'BIND:"New C With {.MissingField = 1}"
                              ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")>
        Public Sub ObjectCreationWithInvalidCollectionInitializer()
            Dim source = <![CDATA[
Class C
    Public Sub M1()
        Dim x1 = New C With {.MissingField = {x = 1}}'BIND:"New C With {.MissingField = {x = 1}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C With  ...  = {x = 1}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: 'With {.Miss ...  = {x = 1}}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.MissingField = {x = 1}')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.MissingField = {x = 1}')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'New C With  ...  = {x = 1}}')
            Right: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{x = 1}')
                Children(1):
                    IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '{x = 1}')
                      Children(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '{x = 1}')
                          IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid, IsImplicit) (Syntax: '{x = 1}')
                            Element Values(1):
                                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: ?, IsInvalid) (Syntax: 'x = 1')
                                  Left: 
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
                                      Children(0)
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'MissingField' is not a member of 'C'.
        Dim x1 = New C With {.MissingField = {x = 1}}'BIND:"New C With {.MissingField = {x = 1}}"
                              ~~~~~~~~~~~~
BC30451: 'x' is not declared. It may be inaccessible due to its protection level.
        Dim x1 = New C With {.MissingField = {x = 1}}'BIND:"New C With {.MissingField = {x = 1}}"
                                              ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")>
        Public Sub ObjectCreationWithInvalidCollectionInitializer02()
            Dim source = <![CDATA[
Class C
    Public Sub M1()
        Dim x1 = New C With {.MissingField = {1}}'BIND:"New C With {.MissingField = {1}}"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C With  ... ield = {1}}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: 'With {.Miss ... ield = {1}}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.MissingField = {1}')
            Left: 
              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.MissingField = {1}')
                Children(1):
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'MissingField')
                      Children(1):
                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'New C With  ... ield = {1}}')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, IsImplicit) (Syntax: '{1}')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: '{1}')
                    Dimension Sizes(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '{1}')
                    Initializer: 
                      IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '{1}')
                        Element Values(1):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'MissingField' is not a member of 'C'.
        Dim x1 = New C With {.MissingField = {1}}'BIND:"New C With {.MissingField = {1}}"
                              ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")>
        Public Sub ObjectCreationCollectionInitializerWithByRefAddMethod()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Implements IEnumerable(Of Integer)

    Public Sub M()
        Dim c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"

    End Sub

    Public Sub Add(ByRef i As Integer)
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation ( Sub C.Add(ByRef i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub C.Add(ByRef i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub C.Add(ByRef i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(22967, "https://github.com/dotnet/roslyn/issues/22967")>
        Public Sub ObjectCreationCollectionInitializerWithOptionalParameterAddMethod()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Implements IEnumerable(Of Integer)

    Public Sub M()
        Dim c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"

    End Sub

    Public Sub Add(i As Integer, Optional o As Object = Nothing)
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation ( Sub C.Add(i As System.Int32, [o As System.Object = Nothing])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub C.Add(i As System.Int32, [o As System.Object = Nothing])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: '2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub C.Add(i As System.Int32, [o As System.Object = Nothing])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(2):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: '3')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerParamArrayAddMethod()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(c As C)
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
        End Sub

        Sub Add(ParamArray ints As Integer())
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation ( Sub Mod1.C.Add(ParamArray ints As System.Int32())) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: '1')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '1')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.C.Add(ParamArray ints As System.Int32())) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: '2')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '2')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '2')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.C.Add(ParamArray ints As System.Int32())) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32(), IsImplicit) (Syntax: '3')
                Dimension Sizes(1):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '3')
                Initializer: 
                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: '3')
                    Element Values(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerExtensionAddMethod()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(c As C)
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class

    <Extension()>
    Sub Add(c As C, i As Integer)
    End Sub
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation ( Sub Mod1.C.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.C.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.C.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerExtensionAddMethodOnInterface()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

Module Mod1
    Interface I
        Inherits IEnumerable(Of Integer)
    End Interface
    Class C
        Implements I

        Sub M(c As C)
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class

    <Extension()>
    Sub Add(c As I, i As Integer)
    End Sub
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation ( Sub Mod1.I.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Mod1.I, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.I.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Mod1.I, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation ( Sub Mod1.I.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Mod1.I, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerAddMethodOnInterface()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

<CoClass(GetType(CoClassImplementation))>
Public Interface IInterface
    Inherits IEnumerable(Of Integer)

    Sub Add(i As Integer)
End Interface

Public Class CoClassImplementation
End Class

Module M
    Sub Main(args() As String)
        Dim i As New IInterface() From {1, 2, 3}'BIND:"From {1, 2, 3}"
    End Sub
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: IInterface) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation (virtual Sub IInterface.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'New IInterf ... m {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual Sub IInterface.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'New IInterf ... m {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (virtual Sub IInterface.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: IInterface, IsImplicit) (Syntax: 'New IInterf ... m {1, 2, 3}')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerLateBoundAddMethod()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(a As Object, b As Object)
            Dim c = New C From {a, b}'BIND:"New C From {a, b}"
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Public Sub Add(i As Integer)
        End Sub

        Public Sub Add(l As Long)
        End Sub
    End Class
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub Mod1.C..ctor()) (OperationKind.ObjectCreation, Type: Mod1.C) (Syntax: 'New C From {a, b}')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {a, b}')
      Initializers(2):
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'a')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'a')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {a, b}')
            Arguments(1):
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')
            ArgumentNames(0)
            ArgumentRefKinds: null
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'b')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'b')
                Type Arguments(0)
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C From {a, b}')
            Arguments(1):
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'b')
            ArgumentNames(0)
            ArgumentRefKinds: null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationWithInitializerAddCallInInitializer()
            ' Ensures that the Add DynamicMemberReference is still treated as explicit
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(a As Object, b As Object)
            Dim c = New C With { .P = .Add(a) }'BIND:".Add(a)"
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function

        Public Property P As Object

        Public Sub Add(i As Integer)
        End Sub

        Public Sub Add(l As Long)
        End Sub
    End Class
End Module]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object) (Syntax: '.Add(a)')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object) (Syntax: '.Add')
      Type Arguments(0)
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: Mod1.C, IsImplicit) (Syntax: 'New C With  ... = .Add(a) }')
  Arguments(1):
      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'a')
  ArgumentNames(0)
  ArgumentRefKinds: null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of InvocationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerSharedAddMethod()
            Dim source = <![CDATA[
Option Strict On
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Mod1
    Class C
        Implements IEnumerable(Of Integer)

        Sub M(c As C)
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
        End Sub

        Shared Sub Add(ints As Integer)
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
            Throw New NotImplementedException()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Throw New NotImplementedException()
        End Function
    End Class
End Module]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
                            ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
                               ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            c = New C From {1, 2, 3}'BIND:"From {1, 2, 3}"
                                  ~
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: Mod1.C) (Syntax: 'From {1, 2, 3}')
  Initializers(3):
      IInvocationOperation (Sub Mod1.C.Add(ints As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (Sub Mod1.C.Add(ints As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IInvocationOperation (Sub Mod1.C.Add(ints As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
        Instance Receiver: 
          null
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: ints) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCollectionInitializerSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ObjectCreationCollectionInitializerBoxingConversion()
            Dim source = <![CDATA[
Imports System.Collections
Imports System.Runtime.CompilerServices
Structure S
    Implements IEnumerable
    Private Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure
Module Program
    <Extension>
    Sub Add(x As Object, y As Integer)
    End Sub
    Sub Main()
        Dim s = New S() From { 1, 2 }'BIND:"New S() From { 1, 2 }"
    End Sub
End Module]]>.Value
            Dim expectedDiagnostics = String.Empty
            Dim expectedOperationTree = <![CDATA[
IObjectCreationOperation (Constructor: Sub S..ctor()) (OperationKind.ObjectCreation, Type: S) (Syntax: 'New S() From { 1, 2 }')
  Arguments(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: S) (Syntax: 'From { 1, 2 }')
      Initializers(2):
          IInvocationOperation ( Sub System.Object.Add(y As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New S() From { 1, 2 }')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: 'New S() From { 1, 2 }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IInvocationOperation ( Sub System.Object.Add(y As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
            Instance Receiver: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New S() From { 1, 2 }')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: S, IsImplicit) (Syntax: 'New S() From { 1, 2 }')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_01()
            Dim source = <![CDATA[
Class C
    Public Sub New(i1 As Integer, i2 As Integer)
    End Sub

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c = New C(1, If(b, 1, 2))
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
    Locals: [c As C]
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
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = New C(1 ... f(b, 1, 2))')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(i1 As System.Int32, i2 As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(1, If(b, 1, 2))')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'If(b, 1, 2)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 1, 2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

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
        Public Sub ObjectCreationFlow_02()
            Dim source = <![CDATA[
Class C
    Public Sub New(i1 As Integer, i2 As Integer)
    End Sub

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c = New C(i2:=2,i1:=If(b, 2, 3))
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
    Locals: [c As C]
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
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = New C(i ... f(b, 2, 3))')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(i1 As System.Int32, i2 As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(i2:=2 ... f(b, 2, 3))')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i1:=If(b, 2, 3)')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i2:=2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

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
        Public Sub ObjectCreationFlow_03()
            Dim source = <![CDATA[
Class C
    Public Sub New(i1 As Integer, i2 As Integer)
    End Sub

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c = New C(i2:=If(b, 2, 3), i1:=1)
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
    Locals: [c As C]
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
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = New C(i ...  3), i1:=1)')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(i1 As System.Int32, i2 As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(i2:=I ...  3), i1:=1)')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: 'i1:=1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'i2:=If(b, 2, 3)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

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
        Public Sub ObjectCreationFlow_04()
            Dim source = <![CDATA[
Class C
    Public Sub New(i1 As Integer, i2 As Integer, Optional i3 As Integer = 3)
    End Sub

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c = New C(1, If(b, 2, 3))
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
    Locals: [c As C]
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
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = New C(1 ... f(b, 2, 3))')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IObjectCreationOperation (Constructor: Sub C..ctor(i1 As System.Int32, i2 As System.Int32, [i3 As System.Int32 = 3])) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(1, If(b, 2, 3))')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null) (Syntax: 'If(b, 2, 3)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: i3) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'C')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'C')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer: 
                    null

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
        Public Sub ObjectCreationFlow_05()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M()'BIND:"Public Sub M()"
        Dim c As C = New C With {.A = 1, .B = 2}
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
    Locals: [c As C]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ...  1, .B = 2}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ...  1, .B = 2}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ...  1, .B = 2}')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningValue)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = 2')
              Left: 
                IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ...  1, .B = 2}')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningValue)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c As C = Ne ...  1, .B = 2}')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ...  1, .B = 2}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_06()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c As C = New C With {.A = 1, .B = If(b, 2, 3)}
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
    Locals: [c As C]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningValue)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = If(b, 2, 3)')
                  Left: 
                    IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'If(b, 2, 3)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c As C = Ne ... f(b, 2, 3)}')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_07()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c As C = New C With {.A = 1}?.A
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [c As C]
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With {.A = 1}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With {.A = 1}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With {.A = 1}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'New C With {.A = 1}')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With {.A = 1}')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.A')
                  Value: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With {.A = 1}')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With {.A = 1}')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'New C With {.A = 1}')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c As C = Ne ... {.A = 1}?.A')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'New C With {.A = 1}?.A')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (NarrowingReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'New C With {.A = 1}?.A')

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
        Public Sub ObjectCreationFlow_08()
            Dim source = <![CDATA[
Class C
    Public Sub New(a As Integer, b As Integer)
    End Sub

    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c As C = New C(1, If(b, 2, 3)) With {.A = 1, .B = If(b, 2, 3)}
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [c As C]
    CaptureIds: [2]
    .locals {R2}
    {
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
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C(1, If ... f(b, 2, 3)}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor(a As System.Int32, b As System.Int32)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C(1, If ... f(b, 2, 3)}')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'If(b, 2, 3)')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer: 
                        null

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C(1, If ... f(b, 2, 3)}')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningValue)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B6]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (0)
            Jump if False (Regular) to Block[B8]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B9]
        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = If(b, 2, 3)')
                  Left: 
                    IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C(1, If ... f(b, 2, 3)}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'If(b, 2, 3)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

            Next (Regular) Block[B10]
                Leaving: {R3}
    }

    Block[B10] - Block
        Predecessors: [B9]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c As C = Ne ... f(b, 2, 3)}')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C(1, If ... f(b, 2, 3)}')

        Next (Regular) Block[B11]
            Leaving: {R1}
}

Block[B11] - Exit
    Predecessors: [B10]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_09()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c As C = New C With {.A = 1, .B = .A}
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
    Locals: [c As C]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... 1, .B = .A}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
              Left: 
                IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningValue)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = .A')
              Left: 
                IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
              Right: 
                IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.A')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c As C = Ne ... 1, .B = .A}')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_10()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c1, c2 As New C With {.A = 1, .B = .A}
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [c1 As C] [c2 As C]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... 1, .B = .A}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = .A')
                  Left: 
                    IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Right: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... 1, .B = .A}')
                  Left: 
                    ILocalReferenceOperation: c1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c1')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... 1, .B = .A}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = .A')
                  Left: 
                    IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')
                  Right: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: '.A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... 1, .B = .A}')
                  Left: 
                    ILocalReferenceOperation: c2 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c2')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... 1, .B = .A}')

            Next (Regular) Block[B3]
                Leaving: {R3} {R1}
    }
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_11()
            Dim source = <![CDATA[
Class C
    Public Property A As Object
    Public Property B As Object

    Public Sub M(b As Boolean)'BIND:"Public Sub M(b As Boolean)"
        Dim c1, c2 As New C With {.A = 1, .B = If(b, 2, 3)}
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [c1 As C] [c2 As C]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... f(b, 2, 3)}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B2]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (0)
                Jump if False (Regular) to Block[B4]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B5]
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = If(b, 2, 3)')
                      Left: 
                        IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'If(b, 2, 3)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningValue)
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... f(b, 2, 3)}')
                  Left: 
                    ILocalReferenceOperation: c1 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c1')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')

            Next (Regular) Block[B7]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [2]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Value: 
                    IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C With  ... f(b, 2, 3)}')
                      Arguments(0)
                      Initializer: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.A = 1')
                  Left: 
                    IPropertyReferenceOperation: Property C.A As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'A')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B8]
                Entering: {R5}

        .locals {R5}
        {
            CaptureIds: [3]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (0)
                Jump if False (Regular) to Block[B10]
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

                Next (Regular) Block[B9]
            Block[B9] - Block
                Predecessors: [B8]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                Next (Regular) Block[B11]
            Block[B10] - Block
                Predecessors: [B8]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

                Next (Regular) Block[B11]
            Block[B11] - Block
                Predecessors: [B9] [B10]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.B = If(b, 2, 3)')
                      Left: 
                        IPropertyReferenceOperation: Property C.B As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'B')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'If(b, 2, 3)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningValue)
                          Operand: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

                Next (Regular) Block[B12]
                    Leaving: {R5}
        }

        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c1, c2 As N ... f(b, 2, 3)}')
                  Left: 
                    ILocalReferenceOperation: c2 (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c2')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C With  ... f(b, 2, 3)}')

            Next (Regular) Block[B13]
                Leaving: {R4} {R1}
    }
}

Block[B13] - Exit
    Predecessors: [B12]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_12()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(c As C2)'BIND:"Sub M(c As C2)"
        c = New C2() From {1, 2, 3}
    End Sub
End Class
Class C2
    Implements IEnumerable(Of Integer)

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i As Integer)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (6)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C2() From {1, 2, 3}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() From {1, 2, 3}')
                  Arguments(0)
                  Initializer: 
                    null

            IInvocationOperation ( Sub C2.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() From {1, 2, 3}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( Sub C2.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() From {1, 2, 3}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( Sub C2.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '3')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() From {1, 2, 3}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C2( ... m {1, 2, 3}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsImplicit) (Syntax: 'c = New C2( ... m {1, 2, 3}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() From {1, 2, 3}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_13()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(c As C2, i1 As Integer?)'BIND:"Sub M(c As C2, i1 As Integer?)"
        c = New C2() From {1, If(i1, 2)}
    End Sub
End Class
Class C2
    Implements IEnumerable(Of Integer)

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i As Integer)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C2() Fr ...  If(i1, 2)}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Fr ...  If(i1, 2)}')
                  Arguments(0)
                  Initializer: 
                    null

            IInvocationOperation ( Sub C2.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ...  If(i1, 2)}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IInvocationOperation ( Sub C2.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'If(i1, 2)')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ...  If(i1, 2)}')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(i1, 2)')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, 2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C2( ...  If(i1, 2)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsImplicit) (Syntax: 'c = New C2( ...  If(i1, 2)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ...  If(i1, 2)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_14()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(c As C2, i1 As Integer?, i2 As Integer?)'BIND:"Sub M(c As C2, i1 As Integer?, i2 As Integer?)"
        c = New C2() From {{1, If(i1, 2)}, {If(i2, 3), 4}}
    End Sub
End Class
Class C2
    Implements IEnumerable(Of Integer)

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i1 As Integer, i2 As Integer)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C2() Fr ... i2, 3), 4}}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C2..ctor()) (OperationKind.ObjectCreation, Type: C2) (Syntax: 'New C2() Fr ... i2, 3), 4}}')
                  Arguments(0)
                  Initializer: 
                    null

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                          Arguments(0)

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IInvocationOperation ( Sub C2.Add(i1 As System.Int32, i2 As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1, If(i1, 2)}')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ... i2, 3), 4}}')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(i1, 2)')
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, 2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B7]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .locals {R4}
    {
        CaptureIds: [6]
        .locals {R5}
        {
            CaptureIds: [5]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                      Value: 
                        IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i2')

                Jump if True (Regular) to Block[B9]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i2')
                    Leaving: {R5}

                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i2')
                          Arguments(0)

                Next (Regular) Block[B10]
                    Leaving: {R5}
        }

        Block[B9] - Block
            Predecessors: [B7]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B8] [B9]
            Statements (1)
                IInvocationOperation ( Sub C2.Add(i1 As System.Int32, i2 As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{If(i2, 3), 4}')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ... i2, 3), 4}}')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(i2, 3)')
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '4')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B11]
                Leaving: {R4}
    }

    Block[B11] - Block
        Predecessors: [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C2( ... i2, 3), 4}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsImplicit) (Syntax: 'c = New C2( ... i2, 3), 4}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'New C2() Fr ... i2, 3), 4}}')

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_15()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Implements IEnumerable(Of Integer)

    Sub M(c As C1, o1 as Object, o2 As Object)'BIND:"Sub M(c As C1, o1 as Object, o2 As Object)"
        c = New C1() From {o1, o2}
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i As Integer)
    End Sub

    Public Sub Add(l As Long)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C1() From {o1, o2}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() From {o1, o2}')
                  Arguments(0)
                  Initializer: 
                    null

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'o1')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() From {o1, o2}')
              Arguments(1):
                  IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
              ArgumentNames(0)
              ArgumentRefKinds: null

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'o2')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() From {o1, o2}')
              Arguments(1):
                  IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
              ArgumentNames(0)
              ArgumentRefKinds: null

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C1( ... om {o1, o2}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'c = New C1( ... om {o1, o2}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() From {o1, o2}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_16()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Implements IEnumerable(Of Integer)

    Sub M(c As C1, o1 as Object, o2 As Object, o3 As Object)'BIND:"Sub M(c As C1, o1 as Object, o2 As Object, o3 As Object)"
        c = New C1() From {o1, If(o2, o3)}
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i As Integer)
    End Sub

    Public Sub Add(l As Long)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C1() Fr ... If(o2, o3)}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Fr ... If(o2, o3)}')
                  Arguments(0)
                  Initializer: 
                    null

            IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'o1')
              Expression: 
                IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                  Type Arguments(0)
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... If(o2, o3)}')
              Arguments(1):
                  IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
              ArgumentNames(0)
              ArgumentRefKinds: null

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                      Value: 
                        IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
                  Value: 
                    IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: 'If(o2, o3)')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: 'If(o2, o3)')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... If(o2, o3)}')
                  Arguments(1):
                      IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(o2, o3)')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C1( ... If(o2, o3)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'c = New C1( ... If(o2, o3)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... If(o2, o3)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_17()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Implements IEnumerable(Of Integer)

    Sub M(c As C1, o1 as Object, o2 As Object, o3 As Object)'BIND:"Sub M(c As C1, o1 as Object, o2 As Object, o3 As Object)"
        c = New C1() From {{1, 2}, {o1, If(o2, o3)}}
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i1 As Integer, i2 As Integer)
    End Sub

    Public Sub Add(l1 As Long, l2 As Long)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C1) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C1() Fr ... f(o2, o3)}}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'New C1() Fr ... f(o2, o3)}}')
                  Arguments(0)
                  Initializer: 
                    null

            IInvocationOperation ( Sub C1.Add(i1 As System.Int32, i2 As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1, 2}')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... f(o2, o3)}}')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value: 
                    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                      Value: 
                        IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o2')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                      Value: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o3')
                  Value: 
                    IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: System.Object, IsImplicit) (Syntax: '{o1, If(o2, o3)}')
                  Expression: 
                    IDynamicMemberReferenceOperation (Member Name: "Add", Containing Type: null) (OperationKind.DynamicMemberReference, Type: System.Object, IsImplicit) (Syntax: '{o1, If(o2, o3)}')
                      Type Arguments(0)
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... f(o2, o3)}}')
                  Arguments(2):
                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                      IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(o2, o3)')
                  ArgumentNames(0)
                  ArgumentRefKinds: null

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = New C1( ... f(o2, o3)}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'c = New C1( ... f(o2, o3)}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'New C1() Fr ... f(o2, o3)}}')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_18()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(c As C2, i1 As Integer)'BIND:"Sub M"
        c = New C2() From {i1}
    End Sub
End Class
Class C2
    Implements IEnumerable(Of Integer)

    Public Sub New(x As Integer)
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(i1 As Integer)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30455: Argument not specified for parameter 'x' of 'Public Sub New(x As Integer)'.
        c = New C2() From {i1}
                ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New C2() From {i1}')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C2, IsInvalid) (Syntax: 'New C2() From {i1}')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'C2')

            IInvocationOperation ( Sub C2.Add(i1 As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'i1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() From {i1}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i1')
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = New C2() From {i1}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsInvalid, IsImplicit) (Syntax: 'c = New C2() From {i1}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2() From {i1}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_19()
            Dim source = <![CDATA[
Imports System

Class C1
    Sub M(c As C2, i1 As Integer, i2 As Integer)'BIND:"Sub M"
        c = New C2(i1) With {.F = i2}
    End Sub
End Class
Class C2

    Public Sub New()
    End Sub

    Public F As Integer
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub New()'.
        c = New C2(i1) With {.F = i2}
                   ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... h {.F = i2}')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C2, IsInvalid) (Syntax: 'New C2(i1)  ... h {.F = i2}')
                  Children(2):
                      IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'C2')
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.F = i2')
              Left: 
                IFieldReferenceOperation: C2.F As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... h {.F = i2}')
              Right: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = New C2( ... h {.F = i2}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsInvalid, IsImplicit) (Syntax: 'c = New C2( ... h {.F = i2}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... h {.F = i2}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_20()
            Dim source = <![CDATA[
Imports System

Class C1
    Sub M(c As C2, i1 As Integer, i2 As Integer, c1 As C1, c2 As C1)'BIND:"Sub M"
        c = New C2(i1, If(c1, c2)) With {.F = i2}
    End Sub
End Class
Class C2

    Public Sub New()
    End Sub

    Public F As Integer
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub New()'.
        c = New C2(i1, If(c1, c2)) With {.F = i2}
                   ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1] [2] [4]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'C2')
                  Value: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'C2')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New C2(i1,  ... h {.F = i2}')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: C2, IsInvalid) (Syntax: 'New C2(i1,  ... h {.F = i2}')
                      Children(3):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: 'C2')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')

            Next (Regular) Block[B7]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.F = i2')
              Left: 
                IFieldReferenceOperation: C2.F As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'F')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1,  ... h {.F = i2}')
              Right: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = New C2( ... h {.F = i2}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsInvalid, IsImplicit) (Syntax: 'c = New C2( ... h {.F = i2}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1,  ... h {.F = i2}')

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ObjectCreationFlow_21()
            Dim source = <![CDATA[
Imports System

Class C1
    Sub M(c As C2, i1 As Integer, c1 As C1, c2 As C1)'BIND:"Sub M"
        c = New C2(i1) With {.F = If(c1, c2)}
    End Sub
End Class
Class C2

    Public Sub New()
    End Sub

    Public F As C1
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30057: Too many arguments to 'Public Sub New()'.
        c = New C2(i1) With {.F = If(c1, c2)}
                   ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C2) (Syntax: 'c')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... If(c1, c2)}')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: C2, IsInvalid) (Syntax: 'New C2(i1)  ... If(c1, c2)}')
                  Children(2):
                      IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'C2')
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [3]
        .locals {R3}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: '.F = If(c1, c2)')
                  Left: 
                    IFieldReferenceOperation: C2.F As C1 (OperationKind.FieldReference, Type: C1) (Syntax: 'F')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... If(c1, c2)}')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = New C2( ... If(c1, c2)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C2, IsInvalid, IsImplicit) (Syntax: 'c = New C2( ... If(c1, c2)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C2, IsImplicit) (Syntax: 'c')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C2, IsInvalid, IsImplicit) (Syntax: 'New C2(i1)  ... If(c1, c2)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
