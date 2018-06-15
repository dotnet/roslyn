' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AnonymousTypesTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldsReferences()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a = 1, .b = .a, .c = .b + .a}'BIND:"New With {.a = 1, .b = .a, .c = .b + .a}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>) (Syntax: 'New With {. ...  = .b + .a}')
  Initializers(3):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = .a')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.a')
            Instance Receiver: 
              null
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.c = .b + .a')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.c As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c')
            Instance Receiver: 
              null
        Right: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '.b + .a')
            Left: 
              IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.b')
                Instance Receiver: 
                  null
            Right: 
              IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.a')
                Instance Receiver: 
                  null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeErrorInFieldReference()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a = sss, .b = .a}'BIND:"New With {.a = sss, .b = .a}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?, b As ?>, IsInvalid) (Syntax: 'New With {. ... s, .b = .a}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = sss')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?, b As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'sss')
            Children(0)
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: '.b = .a')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?, b As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?, b As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: '.a')
            Instance Receiver: 
              null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'sss' is not declared. It may be inaccessible due to its protection level.
        Dim v1 As Object = New With {.a = sss, .b = .a}'BIND:"New With {.a = sss, .b = .a}"
                                          ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldOfRestrictedType()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(tr As System.TypedReference)'BIND:"Sub Test1(tr As System.TypedReference)"
        Dim v1 As Object = New With {.a = tr}
        Dim v2 As Object = New With {.a = {{tr}}}
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (4 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Test1(t ... End Sub')
  Locals: Local_1: v1 As System.Object
    Local_2: v2 As System.Object
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim v1 As O ... h {.a = tr}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'v1 As Objec ... h {.a = tr}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: v1 As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v1')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New With {.a = tr}')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {.a = tr}')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.TypedReference>, IsInvalid) (Syntax: 'New With {.a = tr}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.TypedReference, IsInvalid) (Syntax: '.a = tr')
                      Left: 
                        IPropertyReferenceOperation: Property <anonymous type: a As System.TypedReference>.a As System.TypedReference (OperationKind.PropertyReference, Type: System.TypedReference) (Syntax: 'a')
                          Instance Receiver: 
                            null
                      Right: 
                        IParameterReferenceOperation: tr (OperationKind.ParameterReference, Type: System.TypedReference, IsInvalid) (Syntax: 'tr')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim v2 As O ... a = {{tr}}}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'v2 As Objec ... a = {{tr}}}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: v2 As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'v2')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New With {.a = {{tr}}}')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {.a = {{tr}}}')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.TypedReference(,)>, IsInvalid) (Syntax: 'New With {.a = {{tr}}}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.TypedReference(,), IsInvalid) (Syntax: '.a = {{tr}}')
                      Left: 
                        IPropertyReferenceOperation: Property <anonymous type: a As System.TypedReference(,)>.a As System.TypedReference(,) (OperationKind.PropertyReference, Type: System.TypedReference(,)) (Syntax: 'a')
                          Instance Receiver: 
                            null
                      Right: 
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.TypedReference(,), IsInvalid) (Syntax: '{{tr}}')
                          Dimension Sizes(2):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '{{tr}}')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '{{tr}}')
                          Initializer: 
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid, IsImplicit) (Syntax: '{{tr}}')
                              Element Values(1):
                                  IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null, IsInvalid) (Syntax: '{tr}')
                                    Element Values(1):
                                        IParameterReferenceOperation: tr (OperationKind.ParameterReference, Type: System.TypedReference, IsInvalid) (Syntax: 'tr')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v1 As Object = New With {.a = tr}
                                          ~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v2 As Object = New With {.a = {{tr}}}
                                          ~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim v2 As Object = New With {.a = {{tr}}}
                                          ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeReferenceToOuterTypeField()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim c = New With {.a = 1, .b = New With {.c = .a}}'BIND:"New With {.a = 1, .b = New With {.c = .a}}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>, IsInvalid) (Syntax: 'New With {. ...  {.c = .a}}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: c As ?>, IsInvalid) (Syntax: '.b = New With {.c = .a}')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>.b As <anonymous type: c As ?> (OperationKind.PropertyReference, Type: <anonymous type: c As ?>) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: c As ?>, IsInvalid) (Syntax: 'New With {.c = .a}')
            Initializers(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.c = .a')
                  Left: 
                    IPropertyReferenceOperation: Property <anonymous type: c As ?>.c As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'c')
                      Instance Receiver: 
                        null
                  Right: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.a')
                      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'a' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim c = New With {.a = 1, .b = New With {.c = .a}}'BIND:"New With {.a = 1, .b = New With {.c = .a}}"
                                                      ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder01()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = .b}'BIND:"New With {.b = .c, .c = .b}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: b As ?, c As ?>, IsInvalid) (Syntax: 'New With {. ... c, .c = .b}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.b = .c')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: b As ?, c As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.c')
            Children(0)
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?) (Syntax: '.c = .b')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: b As ?, c As ?>.c As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'c')
            Instance Receiver: 
              null
        Right: 
          IPropertyReferenceOperation: Property <anonymous type: b As ?, c As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: '.b')
            Instance Receiver: 
              null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = .b}'BIND:"New With {.b = .c, .c = .b}"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder02()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = 1}'BIND:"New With {.b = .c, .c = 1}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: b As ?, c As System.Int32>, IsInvalid) (Syntax: 'New With {. ... .c, .c = 1}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.b = .c')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: b As ?, c As System.Int32>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.c')
            Children(0)
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.c = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: b As ?, c As System.Int32>.c As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'c')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = 1}'BIND:"New With {.b = .c, .c = 1}"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithInstanceMethod()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ToString()}'BIND:"New With {.a = .ToString()}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ... ToString()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .ToString()')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.ToString()')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.ToString')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'ToString' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim b = New With {.a = .ToString()}'BIND:"New With {.a = .ToString()}"
                               ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithSharedMethod()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}'BIND:"New With {.a = .ReferenceEquals(Nothing, Nothing)}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ... , Nothing)}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .Refer ... g, Nothing)')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.ReferenceE ... g, Nothing)')
            Children(3):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.ReferenceEquals')
                  Children(0)
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'ReferenceEquals' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}'BIND:"New With {.a = .ReferenceEquals(Nothing, Nothing)}"
                               ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithExtensionMethod()
            Dim source = <![CDATA[
Imports System.Runtime.CompilerServices
Module ModuleA
    Sub Main()
        Dim a = New With {.a = .EM()}'BIND:"New With {.a = .EM()}"
    End Sub
    <Extension()>
    Public Function EM(o As Object) As String
        Return "!"
    End Function
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .EM()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .EM()')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.EM()')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.EM')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'EM' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim a = New With {.a = .EM()}'BIND:"New With {.a = .EM()}"
                               ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithConstructorCall()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a = New With {.a = .New()}'BIND:"New With {.a = .New()}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .New()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .New()')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.New()')
            Children(1):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.New')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'New' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim a = New With {.a = .New()}'BIND:"New With {.a = .New()}"
                               ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldOfVoidType()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a = New With {.a = SubName()}'BIND:"New With {.a = SubName()}"
    End Sub
    Public Sub SubName()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ...  SubName()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = SubName()')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'SubName()')
            Children(1):
                IInvocationOperation (Sub ModuleA.SubName()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'SubName()')
                  Instance Receiver: 
                    null
                  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Dim a = New With {.a = SubName()}'BIND:"New With {.a = SubName()}"
                               ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameWithGeneric()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a = New With {.a = 1, .b = .a(Of Integer)}'BIND:"New With {.a = 1, .b = .a(Of Integer)}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsInvalid) (Syntax: 'New With {. ... f Integer)}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.b = .a(Of Integer)')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.a(Of Integer)')
            Instance Receiver: 
              null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32045: 'Public Property a As T0' has no type parameters and so cannot have type arguments.
        Dim a = New With {.a = 1, .b = .a(Of Integer)}'BIND:"New With {.a = 1, .b = .a(Of Integer)}"
                                         ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldWithSyntaxError()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {.a = .}'BIND:"New With {.a = .}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.')
            Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30203: Identifier expected.
        Dim b = New With {.a = .}'BIND:"New With {.a = .}"
                                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldWithNothingLiteral()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {.a = Nothing}'BIND:"New With {.a = Nothing}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Object>) (Syntax: 'New With {.a = Nothing}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, Constant: null) (Syntax: '.a = Nothing')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Object>.a As System.Object (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromGeneric01()
            Dim source = <![CDATA[
Friend Module AM
    Sub Main()
        Dim at = New With {New A().F(Of Integer)}'BIND:"New With {New A().F(Of Integer)}"
    End Sub

    Class A
        Public Function F(Of T)() As T
            Return Nothing
        End Function
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As System.Int32>, IsInvalid) (Syntax: 'New With {N ... f Integer)}')
  Initializers(1):
      IInvocationOperation ( Function AM.A.F(Of System.Int32)() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'New A().F(Of Integer)')
        Instance Receiver: 
          IObjectCreationOperation (Constructor: Sub AM.A..ctor()) (OperationKind.ObjectCreation, Type: AM.A, IsInvalid) (Syntax: 'New A()')
            Arguments(0)
            Initializer: 
              null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim at = New With {New A().F(Of Integer)}'BIND:"New With {New A().F(Of Integer)}"
                           ~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml01()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name></some-name>}'BIND:"New With {<some-name></some-name>}"
    End Sub
End Module
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As System.Xml.Linq.XElement>, IsInvalid) (Syntax: 'New With {< ... some-name>}')
  Initializers(1):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '<some-name></some-name>')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With {<some-name></some-name>}'BIND:"New With {<some-name></some-name>}"
                          ~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:=XmlReferences)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml02()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name></some-name>.@aa}'BIND:"New With {<some-name></some-name>.@aa}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: aa As System.String>) (Syntax: 'New With {< ... -name>.@aa}')
  Initializers(1):
      IOperation:  (OperationKind.None, Type: null) (Syntax: '<some-name> ... e-name>.@aa')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:=XmlReferences)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml03()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}'BIND:"New With {<some-name name="a"></some-name>.@<a-a>}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As System.String>, IsInvalid) (Syntax: 'New With {< ... me>.@<a-a>}')
  Initializers(1):
      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '<some-name  ... ame>.@<a-a>')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}'BIND:"New With {<some-name name="a"></some-name>.@<a-a>}"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, references:=XmlReferences)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(544370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544370")>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml04()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()'BIND:"Sub Main()"
        Dim err = New With {<a/>.<_>}
        Dim ok = New With {<a/>.<__>}
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IBlockOperation (4 statements, 2 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: err As <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
    Local_2: ok As <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim err = N ...  {<a/>.<_>}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'err = New W ...  {<a/>.<_>}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: err As <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'err')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New With {<a/>.<_>}')
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>, IsInvalid) (Syntax: 'New With {<a/>.<_>}')
            Initializers(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '<a/>.<_>')
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Dim ok = Ne ... {<a/>.<__>}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'ok = New Wi ... {<a/>.<__>}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: ok As <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'ok')
            Initializer: 
              null
      Initializer: 
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= New With {<a/>.<__>}')
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>) (Syntax: 'New With {<a/>.<__>}')
            Initializers(1):
                IOperation:  (OperationKind.None, Type: null) (Syntax: '<a/>.<__>')
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement: 
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue: 
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim err = New With {<a/>.<_>}
                            ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics, references:=XmlReferences)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression01()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a As Integer = 0
        Dim b = New With {a * 2}'BIND:"New With {a * 2}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As System.Int32>, IsInvalid) (Syntax: 'New With {a * 2}')
  Initializers(1):
      IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32, IsInvalid) (Syntax: 'a * 2')
        Left: 
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With {a * 2}'BIND:"New With {a * 2}"
                          ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression02()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a As Integer = 0
        Dim b = New With {.a = 1, a}'BIND:"New With {.a = 1, a}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, a As System.Int32>, IsInvalid) (Syntax: 'New With {.a = 1, a}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, a As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'a' is already declared.
        Dim b = New With {.a = 1, a}'BIND:"New With {.a = 1, a}"
                                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression03()
            Dim source = <![CDATA[
Module ModuleA
    Structure S
        Public Property FLD As Integer
    End Structure
    Sub Main()
        Dim a As S = New S()
        Dim b = New With {a.FLD, a.FLD()}'BIND:"New With {a.FLD, a.FLD()}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: FLD As System.Int32, FLD As System.Int32>, IsInvalid) (Syntax: 'New With {a ... D, a.FLD()}')
  Initializers(2):
      IPropertyReferenceOperation: Property ModuleA.S.FLD As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a.FLD')
        Instance Receiver: 
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: ModuleA.S) (Syntax: 'a')
      IPropertyReferenceOperation: Property ModuleA.S.FLD As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'a.FLD()')
        Instance Receiver: 
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: ModuleA.S, IsInvalid) (Syntax: 'a')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'FLD' is already declared.
        Dim b = New With {a.FLD, a.FLD()}'BIND:"New With {a.FLD, a.FLD()}"
                                 ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromExpression04()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Module ModuleA
    Sub Main()
        Dim a As New Dictionary(Of String, Integer)
        Dim b = New With {.x = 1, a!x}'BIND:"New With {.x = 1, a!x}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: x As System.Int32, x As System.Int32>, IsInvalid) (Syntax: 'New With {.x = 1, a!x}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.x = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: x As System.Int32, x As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IPropertyReferenceOperation: Property System.Collections.Generic.Dictionary(Of System.String, System.Int32).Item(key As System.String) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'a!x')
        Instance Receiver: 
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsInvalid) (Syntax: 'a')
        Arguments(1):
            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "x", IsInvalid) (Syntax: 'x')
              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'x' is already declared.
        Dim b = New With {.x = 1, a!x}'BIND:"New With {.x = 1, a!x}"
                                  ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithAddressOf()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {Key .a = AddressOf S})'BIND:"New With {Key .a = AddressOf S}"
    End Sub
    Sub S()
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As ?>, IsInvalid) (Syntax: 'New With {K ... ddressOf S}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Key .a = AddressOf S')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'AddressOf S')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'AddressOf S')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'S')
                        Children(1):
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsInvalid, IsImplicit) (Syntax: 'S')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Console.WriteLine(New With {Key .a = AddressOf S})'BIND:"New With {Key .a = AddressOf S}"
                                             ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate01()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {'BIND:"New With {"
                          Key .x = "--value--",
                          Key .a = DirectCast(Function() As String
                                                  Return .x.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module]]>.Value

            ' The IOperation tree for this test seems to have an unexpected ILocalReferenceExpression within IAnonymousFunctionExpression.
            ' See https://github.com/dotnet/roslyn/issues/20357.
            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.String, Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(2):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: "--value--") (Syntax: 'Key .x = "--value--"')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.x As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "--value--") (Syntax: '"--value--"')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.a As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvocationOperation (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return .x.ToString()')
                        ReturnedValue: 
                          IInvocationOperation (virtual Function System.String.ToString() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: '.x.ToString()')
                            Instance Receiver: 
                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.x As System.String (OperationKind.PropertyReference, Type: System.String, IsInvalid) (Syntax: '.x')
                                Instance Receiver: 
                                  null
                            Arguments(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36549: Anonymous type property 'x' cannot be used in the definition of a lambda expression within the same initialization list.
                                                  Return .x.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate02()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {'BIND:"New With {"
                          Key .a = DirectCast(Function() As String
                                                  Return .a.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module]]>.Value

            ' The IOperation tree for this test seems to have an unexpected ILocalReferenceExpression within IAnonymousFunctionExpression.
            ' See https://github.com/dotnet/roslyn/issues/20357.
            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvocationOperation (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return .a.ToString()')
                        ReturnedValue: 
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '.a.ToString()')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.a.ToString()')
                                Children(1):
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.a.ToString')
                                      Children(1):
                                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.a.ToString')
                                            Children(1):
                                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.a')
                                                  Children(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'a' cannot be used to infer the type of another member property because the type of 'a' is not yet established.
                                                  Return .a.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate03()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {'BIND:"New With {"
                          Key .a = DirectCast(Function() As String
                                                  Return .x.ToString()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module]]>.Value

            ' The IOperation tree for this test seems to have an unexpected ILocalReferenceExpression within IAnonymousFunctionExpression.
            ' See https://github.com/dotnet/roslyn/issues/20357.
            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvocationOperation (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return .x.ToString()')
                        ReturnedValue: 
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '.x.ToString()')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: 
                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x.ToString()')
                                Children(1):
                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.x.ToString')
                                      Children(1):
                                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                            Children(1):
                                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x')
                                                  Children(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'x' is not a member of '<anonymous type>'; it does not exist in the current context.
                                                  Return .x.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithDelegate04()
            Dim source = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine(New With {'BIND:"New With {"
                          Key .a = DirectCast(Function() As String
                                                  Return DirectCast(Function() As String
                                                                        Return .x.ToString()
                                                                    End Function, Func(Of String)).Invoke()
                                              End Function, Func(Of String)).Invoke()})
    End Sub
End Module]]>.Value

            ' The IOperation tree for this test seems to have an unexpected ILocalReferenceExpression within IAnonymousFunctionExpression.
            ' See https://github.com/dotnet/roslyn/issues/20357.
            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'a')
            Instance Receiver: 
              null
        Right: 
          IInvocationOperation (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: 
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Target: 
                  IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return Dire ... )).Invoke()')
                        ReturnedValue: 
                          IInvocationOperation (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
                            Instance Receiver: 
                              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                                Target: 
                                  IAnonymousFunctionOperation (Symbol: Function () As System.String) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                                    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function()  ... nd Function')
                                      Locals: Local_1: <anonymous local> As System.String
                                      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'Return .x.ToString()')
                                        ReturnedValue: 
                                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '.x.ToString()')
                                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            Operand: 
                                              IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x.ToString()')
                                                Children(1):
                                                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.x.ToString')
                                                      Children(1):
                                                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                                            Children(1):
                                                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.x')
                                                                  Children(0)
                                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                                        Statement: 
                                          null
                                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                                        ReturnedValue: 
                                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'End Function')
                            Arguments(0)
                      ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Function')
                        Statement: 
                          null
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Function')
                        ReturnedValue: 
                          ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'x' is not a member of '<anonymous type>'; it does not exist in the current context.
                                                                        Return .x.ToString()
                                                                               ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(542940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542940")>
        <Fact>
        Public Sub LambdaReturningAnonymousType()
            Dim source = <![CDATA[
Module Program
    Sub Main(args As String())
        Dim x1 As Object = Function() New With {.Default = "Test"}'BIND:"New With {.Default = "Test"}"
        System.Console.WriteLine(x1)
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Default As System.String>) (Syntax: 'New With {. ... t = "Test"}')
  Initializers(1):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: "Test") (Syntax: '.Default = "Test"')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: Default As System.String>.Default As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'Default')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Test") (Syntax: '"Test"')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/27338")>
        Public Sub AnonymousTypeCreation_MixedInitializers()
            Dim source = <![CDATA[
Module Program
    Sub M(a As Integer, o As Object)
        o = New With { a, .b = 1 }'BIND:"New With { a, .b = 1 }"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With { a, .b = 1 }')
  Initializers(2):
      IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = 1')
        Left: 
          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
            Instance Receiver: 
              null
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <WorkItem(543286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543286")>
        <Fact>
        Public Sub AnonymousTypeInALambdaInGenericMethod1()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod1">
    <file name="a.vb">
Imports System

Module S1
    Public Function Goo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        return Function()
                     Return new With {x2}
               End Function
    End Function

    Sub Main()
        Console.WriteLine(Goo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AnonymousTypeInALambdaInGenericMethod1_OperationTree()
            Dim source = <![CDATA[
Imports System

Module S1
    Public Function Foo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        Return Function()
                   Return New With {x2}'BIND:"New With {x2}"
               End Function
    End Function

    Sub Main()
        Console.WriteLine(Foo(Of Integer)()())
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: x2 As T>) (Syntax: 'New With {x2}')
  Initializers(1):
      ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: T) (Syntax: 'x2')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <WorkItem(543286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543286")>
        <Fact>
        Public Sub AnonymousTypeInALambdaInGenericMethod2()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod2">
    <file name="a.vb">
Imports System

Module S1
    Public Function Goo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        Dim x3 = Function()
                     Dim result = new With {x2}
                     Return result
               End Function

        return x3
    End Function

    Sub Main()
        Console.WriteLine(Goo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <Fact()>
        Public Sub AnonymousTypeInALambdaInGenericMethod3()
            Dim compilationDef =
<compilation name="AnonymousTypeInALambdaInGenericMethod3">
    <file name="a.vb">
Imports System

Module S1
    Public Function Goo(Of T)() As System.Func(Of Object)
        Dim x2 As T = Nothing
        Dim x3 = Function()
                     Dim result = new With {x2}
                     Dim tmp = result.x2 ' Property getter should be also rewritten
                     Return result
               End Function

        return x3
    End Function

    Sub Main()
        Console.WriteLine(Goo(Of Integer)()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Valid()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Goo()
        Dim sss = Sub(x) Console.WriteLine() 'BIND2:"x"
        sss(x:=1)'BIND1:"sss(x:=1)"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            Assert.NotNull(associatedDelegate)
            Assert.True(associatedDelegate.IsDelegateType)
            Assert.True(associatedDelegate.IsAnonymousType)
            Assert.Equal("Sub <generated method>(x As Object)", associatedDelegate.ToDisplayString)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Sub Invoke(x As Object)", info.Symbol.ToDisplayString())
            Assert.Equal("Sub <generated method>(x As Object)", info.Symbol.ContainingSymbol.ToDisplayString())

            Assert.Same(associatedDelegate, info.Symbol.ContainingSymbol)
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Action_SameSignature()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Goo()
        Dim sss As Action(Of Object) = Sub(x) Console.WriteLine() 'BIND2:"x"
        sss(obj:=1)'BIND1:"sss(obj:=1)"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            'Assert.Null(associatedDelegate)
            Assert.True(associatedDelegate.IsDelegateType)
            Assert.True(associatedDelegate.IsAnonymousType)
            Assert.Equal("Sub <generated method>(x As Object)", associatedDelegate.ToDisplayString)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Overloads Sub Invoke(obj As Object)", info.Symbol.ToDisplayString())
            Assert.Equal("System.Action(Of Object)", info.Symbol.ContainingSymbol.ToDisplayString())

            Assert.NotSame(associatedDelegate, info.Symbol.ContainingSymbol)
        End Sub

        <WorkItem(529688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529688")>
        <Fact()>
        Public Sub AssociatedAnonymousDelegate_Action_DifferentSignature()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module S1
    Public Sub Goo()
        Dim sss As Action = Sub(x) Console.WriteLine() 'BIND2:"x"
        sss()'BIND1:"sss()"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node2 As ParameterSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 2)
            Dim symbol2 = model.GetDeclaredSymbol(node2)
            Assert.Equal(SymbolKind.Parameter, symbol2.Kind)
            Assert.Equal("x As Object", symbol2.ToDisplayString())

            Dim lambda2 = DirectCast(symbol2.ContainingSymbol, MethodSymbol)
            Assert.Equal(MethodKind.LambdaMethod, lambda2.MethodKind)
            Assert.Equal("Private Shared Sub (x As Object)", symbol2.ContainingSymbol.ToDisplayString())

            Dim associatedDelegate = lambda2.AssociatedAnonymousDelegate
            Assert.Null(associatedDelegate)
            Assert.Same(associatedDelegate, DirectCast(lambda2, IMethodSymbol).AssociatedAnonymousDelegate)

            Dim node As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)
            Dim info = model.GetSymbolInfo(node)
            Assert.Equal("Public Overridable Overloads Sub Invoke()", info.Symbol.ToDisplayString())
            Assert.Equal("System.Action", info.Symbol.ContainingSymbol.ToDisplayString())
        End Sub
    End Class

End Namespace


