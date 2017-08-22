' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class AnonymousTypesTests
        Inherits BasicTestBase

        <Fact>
        Public Sub AnonymousTypeFieldsReferences()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a = 1, .b = .a, .c = .b + .a}'BIND:"New With {.a = 1, .b = .a, .c = .b + .a}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'New With {. ...  = .b + .a}')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>) (Syntax: 'New With {. ...  = .b + .a}')
      Initializers(3):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
            Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'a')
                Instance Receiver: null
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '.b = .a')
            Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.b As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'b')
                Instance Receiver: null
            Right: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: '.a')
                Instance Receiver: null
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '.c = .b + .a')
            Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.c As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'c')
                Instance Receiver: null
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '.b + .a')
                Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.b As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: '.b')
                    Instance Receiver: null
                Right: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32, c As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: '.a')
                    Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeErrorInFieldReference()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim v1 As Object = New With {.a = sss, .b = .a}'BIND:"New With {.a = sss, .b = .a}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'New With {. ... s, .b = .a}')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?, b As ?>, IsInvalid) (Syntax: 'New With {. ... s, .b = .a}')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = sss')
            Left: IPropertyReferenceExpression: Property <anonymous type: a As ?, b As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
                Instance Receiver: null
            Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'sss')
                Children(0)
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?) (Syntax: '.b = .a')
            Left: IPropertyReferenceExpression: Property <anonymous type: a As ?, b As ?>.b As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'b')
                Instance Receiver: null
            Right: IPropertyReferenceExpression: Property <anonymous type: a As ?, b As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: '.a')
                Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30451: 'sss' is not declared. It may be inaccessible due to its protection level.
        Dim v1 As Object = New With {.a = sss, .b = .a}'BIND:"New With {.a = sss, .b = .a}"
                                          ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IBlockStatement (4 statements, 2 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub Test1(t ... End Sub')
  Locals: Local_1: v1 As System.Object
    Local_2: v2 As System.Object
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim v1 As O ... h {.a = tr}')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'v1')
      Variables: Local_1: v1 As System.Object
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'New With {.a = tr}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.TypedReference>, IsInvalid) (Syntax: 'New With {.a = tr}')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.TypedReference, IsInvalid) (Syntax: '.a = tr')
                    Left: IPropertyReferenceExpression: Property <anonymous type: a As System.TypedReference>.a As System.TypedReference (Static) (OperationKind.PropertyReferenceExpression, Type: System.TypedReference) (Syntax: 'a')
                        Instance Receiver: null
                    Right: IParameterReferenceExpression: tr (OperationKind.ParameterReferenceExpression, Type: System.TypedReference, IsInvalid) (Syntax: 'tr')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim v2 As O ... a = {{tr}}}')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'v2')
      Variables: Local_1: v2 As System.Object
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'New With {.a = {{tr}}}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.TypedReference(,)>, IsInvalid) (Syntax: 'New With {.a = {{tr}}}')
              Initializers(1):
                  ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.TypedReference(,), IsInvalid) (Syntax: '.a = {{tr}}')
                    Left: IPropertyReferenceExpression: Property <anonymous type: a As System.TypedReference(,)>.a As System.TypedReference(,) (Static) (OperationKind.PropertyReferenceExpression, Type: System.TypedReference(,)) (Syntax: 'a')
                        Instance Receiver: null
                    Right: IArrayCreationExpression (Element Type: System.TypedReference) (OperationKind.ArrayCreationExpression, Type: System.TypedReference(,), IsInvalid) (Syntax: '{{tr}}')
                        Dimension Sizes(2):
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '{{tr}}')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '{{tr}}')
                        Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{{tr}}')
                            Element Values(1):
                                IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid) (Syntax: '{tr}')
                                  Element Values(1):
                                      IParameterReferenceExpression: tr (OperationKind.ParameterReferenceExpression, Type: System.TypedReference, IsInvalid) (Syntax: 'tr')
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
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

        <Fact>
        Public Sub AnonymousTypeReferenceToOuterTypeField()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1()
        Dim c = New With {.a = 1, .b = New With {.c = .a}}'BIND:"New With {.a = 1, .b = New With {.c = .a}}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>, IsInvalid) (Syntax: 'New With {. ...  {.c = .a}}')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: <anonymous type: c As ?>, IsInvalid) (Syntax: '.b = New With {.c = .a}')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As <anonymous type: c As ?>>.b As <anonymous type: c As ?> (Static) (OperationKind.PropertyReferenceExpression, Type: <anonymous type: c As ?>) (Syntax: 'b')
            Instance Receiver: null
        Right: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: c As ?>, IsInvalid) (Syntax: 'New With {.c = .a}')
            Initializers(1):
                ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.c = .a')
                  Left: IPropertyReferenceExpression: Property <anonymous type: c As ?>.c As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'c')
                      Instance Receiver: null
                  Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.a')
                      Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'a' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim c = New With {.a = 1, .b = New With {.c = .a}}'BIND:"New With {.a = 1, .b = New With {.c = .a}}"
                                                      ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder01()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = .b}'BIND:"New With {.b = .c, .c = .b}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'New With {. ... c, .c = .b}')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: b As ?, c As ?>, IsInvalid) (Syntax: 'New With {. ... c, .c = .b}')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.b = .c')
            Left: IPropertyReferenceExpression: Property <anonymous type: b As ?, c As ?>.b As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'b')
                Instance Receiver: null
            Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.c')
                Children(0)
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?) (Syntax: '.c = .b')
            Left: IPropertyReferenceExpression: Property <anonymous type: b As ?, c As ?>.c As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'c')
                Instance Receiver: null
            Right: IPropertyReferenceExpression: Property <anonymous type: b As ?, c As ?>.b As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: '.b')
                Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = .b}'BIND:"New With {.b = .c, .c = .b}"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldReferenceOutOfOrder02()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim v1 As Object = New With {.b = .c, .c = 1}'BIND:"New With {.b = .c, .c = 1}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'New With {. ... .c, .c = 1}')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: b As ?, c As System.Int32>, IsInvalid) (Syntax: 'New With {. ... .c, .c = 1}')
      Initializers(2):
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.b = .c')
            Left: IPropertyReferenceExpression: Property <anonymous type: b As ?, c As System.Int32>.b As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'b')
                Instance Receiver: null
            Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.c')
                Children(0)
          ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.c = 1')
            Left: IPropertyReferenceExpression: Property <anonymous type: b As ?, c As System.Int32>.c As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'c')
                Instance Receiver: null
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'c' cannot be used to infer the type of another member property because the type of 'c' is not yet established.
        Dim v1 As Object = New With {.b = .c, .c = 1}'BIND:"New With {.b = .c, .c = 1}"
                                          ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithInstanceMethod()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ToString()}'BIND:"New With {.a = .ToString()}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ... ToString()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = .ToString()')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.ToString()')
            Children(1):
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.ToString')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'ToString' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim b = New With {.a = .ToString()}'BIND:"New With {.a = .ToString()}"
                               ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithSharedMethod()
            Dim source = <![CDATA[
Module ModuleA
    Sub Test1(x As Integer)
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}'BIND:"New With {.a = .ReferenceEquals(Nothing, Nothing)}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ... , Nothing)}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = .Refer ... g, Nothing)')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.ReferenceE ... g, Nothing)')
            Children(3):
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.ReferenceEquals')
                  Children(0)
                ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
                ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'ReferenceEquals' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim b = New With {.a = .ReferenceEquals(Nothing, Nothing)}'BIND:"New With {.a = .ReferenceEquals(Nothing, Nothing)}"
                               ~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .EM()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = .EM()')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.EM()')
            Children(1):
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.EM')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'EM' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim a = New With {.a = .EM()}'BIND:"New With {.a = .EM()}"
                               ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldInitializedWithConstructorCall()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a = New With {.a = .New()}'BIND:"New With {.a = .New()}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .New()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = .New()')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.New()')
            Children(1):
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.New')
                  Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'New' is not a member of '<anonymous type>'; it does not exist in the current context.
        Dim a = New With {.a = .New()}'BIND:"New With {.a = .New()}"
                               ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {. ...  SubName()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = SubName()')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'SubName()')
            Children(1):
                IInvocationExpression (Sub ModuleA.SubName()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'SubName()')
                  Instance Receiver: null
                  Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Dim a = New With {.a = SubName()}'BIND:"New With {.a = SubName()}"
                               ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameWithGeneric()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim a = New With {.a = 1, .b = .a(Of Integer)}'BIND:"New With {.a = 1, .b = .a(Of Integer)}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsInvalid) (Syntax: 'New With {. ... f Integer)}')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, IsInvalid) (Syntax: '.b = .a(Of Integer)')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'b')
            Instance Receiver: null
        Right: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: '.a(Of Integer)')
            Instance Receiver: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32045: 'Public Property a As T0' has no type parameters and so cannot have type arguments.
        Dim a = New With {.a = 1, .b = .a(Of Integer)}'BIND:"New With {.a = 1, .b = .a(Of Integer)}"
                                         ~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldWithSyntaxError()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {.a = .}'BIND:"New With {.a = .}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = .}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: '.a = .')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.')
            Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30203: Identifier expected.
        Dim b = New With {.a = .}'BIND:"New With {.a = .}"
                                ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldWithNothingLiteral()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {.a = Nothing}'BIND:"New With {.a = Nothing}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.Object>) (Syntax: 'New With {.a = Nothing}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Object, Constant: null) (Syntax: '.a = Nothing')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Object>.a As System.Object (Static) (OperationKind.PropertyReferenceExpression, Type: System.Object) (Syntax: 'a')
            Instance Receiver: null
        Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: $0 As System.Int32>, IsInvalid) (Syntax: 'New With {N ... f Integer)}')
  Initializers(1):
      IInvocationExpression ( Function AM.A.F(Of System.Int32)() As System.Int32) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'New A().F(Of Integer)')
        Instance Receiver: IObjectCreationExpression (Constructor: Sub AM.A..ctor()) (OperationKind.ObjectCreationExpression, Type: AM.A, IsInvalid) (Syntax: 'New A()')
            Arguments(0)
            Initializer: null
        Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim at = New With {New A().F(Of Integer)}'BIND:"New With {New A().F(Of Integer)}"
                           ~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: $0 As System.Xml.Linq.XElement>, IsInvalid) (Syntax: 'New With {< ... some-name>}')
  Initializers(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: '<some-name></some-name>')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With {<some-name></some-name>}'BIND:"New With {<some-name></some-name>}"
                          ~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, additionalReferences:=XmlReferences)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml02()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name></some-name>.@aa}'BIND:"New With {<some-name></some-name>.@aa}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: aa As System.String>) (Syntax: 'New With {< ... -name>.@aa}')
  Initializers(1):
      IOperation:  (OperationKind.None) (Syntax: '<some-name> ... e-name>.@aa')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, additionalReferences:=XmlReferences)
        End Sub

        <Fact>
        Public Sub AnonymousTypeFieldNameInferenceFromXml03()
            Dim source = <![CDATA[
Module ModuleA
    Sub Main()
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}'BIND:"New With {<some-name name="a"></some-name>.@<a-a>}"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: $0 As System.String>, IsInvalid) (Syntax: 'New With {< ... me>.@<a-a>}')
  Initializers(1):
      IOperation:  (OperationKind.None, IsInvalid) (Syntax: '<some-name  ... ame>.@<a-a>')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim b = New With {<some-name name="a"></some-name>.@<a-a>}'BIND:"New With {<some-name name="a"></some-name>.@<a-a>}"
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics, additionalReferences:=XmlReferences)
        End Sub

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
IBlockStatement (4 statements, 2 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Sub Main()' ... End Sub')
  Locals: Local_1: err As <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
    Local_2: ok As <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Dim err = N ...  {<a/>.<_>}')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'err')
      Variables: Local_1: err As <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
      Initializer: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: $0 As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>, IsInvalid) (Syntax: 'New With {<a/>.<_>}')
          Initializers(1):
              IOperation:  (OperationKind.None, IsInvalid) (Syntax: '<a/>.<_>')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Dim ok = Ne ... {<a/>.<__>}')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ok')
      Variables: Local_1: ok As <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>
      Initializer: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: __ As System.Collections.Generic.IEnumerable(Of System.Xml.Linq.XElement)>) (Syntax: 'New With {<a/>.<__>}')
          Initializers(1):
              IOperation:  (OperationKind.None) (Syntax: '<a/>.<__>')
  ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Sub')
    LabeledStatement: null
  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Sub')
    ReturnedValue: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36613: Anonymous type member name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        Dim err = New With {<a/>.<_>}
                            ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics, additionalReferences:=XmlReferences)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: $0 As System.Int32>, IsInvalid) (Syntax: 'New With {a * 2}')
  Initializers(1):
      IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'a * 2')
        Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'a')
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim b = New With {a * 2}'BIND:"New With {a * 2}"
                          ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: a As System.Int32, a As System.Int32>, IsInvalid) (Syntax: 'New With {.a = 1, a}')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
        Left: IPropertyReferenceExpression: Property <anonymous type: a As System.Int32, a As System.Int32>.a As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'a')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'a' is already declared.
        Dim b = New With {.a = 1, a}'BIND:"New With {.a = 1, a}"
                                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: FLD As System.Int32, FLD As System.Int32>, IsInvalid) (Syntax: 'New With {a ... D, a.FLD()}')
  Initializers(2):
      IPropertyReferenceExpression: Property ModuleA.S.FLD As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'a.FLD')
        Instance Receiver: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: ModuleA.S) (Syntax: 'a')
      IPropertyReferenceExpression: Property ModuleA.S.FLD As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'a.FLD()')
        Instance Receiver: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: ModuleA.S, IsInvalid) (Syntax: 'a')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'FLD' is already declared.
        Dim b = New With {a.FLD, a.FLD()}'BIND:"New With {a.FLD, a.FLD()}"
                                 ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: x As System.Int32, x As System.Int32>, IsInvalid) (Syntax: 'New With {.x = 1, a!x}')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32, Constant: 1) (Syntax: '.x = 1')
        Left: IPropertyReferenceExpression: Property <anonymous type: x As System.Int32, x As System.Int32>.x As System.Int32 (Static) (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IPropertyReferenceExpression: Property System.Collections.Generic.Dictionary(Of System.String, System.Int32).Item(key As System.String) As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'a!x')
        Instance Receiver: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.Dictionary(Of System.String, System.Int32), IsInvalid) (Syntax: 'a')
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: key) (OperationKind.Argument, IsInvalid) (Syntax: 'x')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "x", IsInvalid) (Syntax: 'x')
              InConversion: null
              OutConversion: null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'x' is already declared.
        Dim b = New With {.x = 1, a!x}'BIND:"New With {.x = 1, a!x}"
                                  ~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key a As ?>, IsInvalid) (Syntax: 'New With {K ... ddressOf S}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: ?, IsInvalid) (Syntax: 'Key .a = AddressOf S')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key a As ?>.a As ? (Static) (OperationKind.PropertyReferenceExpression, Type: ?) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'AddressOf S')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'AddressOf S')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30491: Expression does not produce a value.
        Console.WriteLine(New With {Key .a = AddressOf S})'BIND:"New With {Key .a = AddressOf S}"
                                             ~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key x As System.String, Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(2):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, Constant: "--value--") (Syntax: 'Key .x = "--value--"')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.x As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "--value--") (Syntax: '"--value--"')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.a As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvocationExpression (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return .x.ToString()')
                        ReturnedValue: IInvocationExpression (virtual Function System.String.ToString() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: '.x.ToString()')
                            Instance Receiver: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key x As System.String, Key a As System.String>.x As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String, IsInvalid) (Syntax: '.x')
                                Instance Receiver: null
                            Arguments(0)
                      ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
                        LabeledStatement: null
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                        ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36549: Anonymous type property 'x' cannot be used in the definition of a lambda expression within the same initialization list.
                                                  Return .x.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvocationExpression (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return .a.ToString()')
                        ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '.a.ToString()')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.a.ToString()')
                                Children(1):
                                    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.a.ToString')
                                      Children(1):
                                          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.a.ToString')
                                            Children(1):
                                                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.a')
                                                  Children(0)
                      ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
                        LabeledStatement: null
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                        ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'a' cannot be used to infer the type of another member property because the type of 'a' is not yet established.
                                                  Return .a.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvocationExpression (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return .x.ToString()')
                        ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '.x.ToString()')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString()')
                                Children(1):
                                    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                      Children(1):
                                          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                            Children(1):
                                                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x')
                                                  Children(0)
                      ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
                        LabeledStatement: null
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                        ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'x' is not a member of '<anonymous type>'; it does not exist in the current context.
                                                  Return .x.ToString()
                                                         ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Key a As System.String>, IsInvalid) (Syntax: 'New With {' ... ).Invoke()}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'Key .a = Di ... )).Invoke()')
        Left: IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key a As System.String>.a As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'a')
            Instance Receiver: null
        Right: IInvocationExpression (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
            Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()  ... nd Function')
                      Locals: Local_1: <anonymous local> As System.String
                      IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return Dire ... )).Invoke()')
                        ReturnedValue: IInvocationExpression (virtual Function System.Func(Of System.String).Invoke() As System.String) (OperationKind.InvocationExpression, Type: System.String, IsInvalid) (Syntax: 'DirectCast( ... )).Invoke()')
                            Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func(Of System.String), IsInvalid) (Syntax: 'DirectCast( ... Of String))')
                                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Operand: IAnonymousFunctionExpression (Symbol: Function () As System.String) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'Function()  ... nd Function')
                                    IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'Function()  ... nd Function')
                                      Locals: Local_1: <anonymous local> As System.String
                                      IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'Return .x.ToString()')
                                        ReturnedValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '.x.ToString()')
                                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString()')
                                                Children(1):
                                                    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                                      Children(1):
                                                          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x.ToString')
                                                            Children(1):
                                                                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '.x')
                                                                  Children(0)
                                      ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
                                        LabeledStatement: null
                                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                                        ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
                            Arguments(0)
                      ILabelStatement (Label: exit) (OperationKind.LabelStatement) (Syntax: 'End Function')
                        LabeledStatement: null
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'End Function')
                        ReturnedValue: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'End Function')
            Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36557: 'x' is not a member of '<anonymous type>'; it does not exist in the current context.
                                                                        Return .x.ToString()
                                                                               ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AnonymousObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

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
IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: Default As System.String>) (Syntax: 'New With {. ... t = "Test"}')
  Initializers(1):
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String, Constant: "Test") (Syntax: '.Default = "Test"')
        Left: IPropertyReferenceExpression: Property <anonymous type: Default As System.String>.Default As System.String (Static) (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'Default')
            Instance Receiver: null
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Test") (Syntax: '"Test"')
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="{ x2 = 0 }")
        End Sub

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
IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'New With {x2}')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  Operand: IAnonymousObjectCreationExpression (OperationKind.AnonymousObjectCreationExpression, Type: <anonymous type: x2 As T>) (Syntax: 'New With {x2}')
      Initializers(1):
          ILocalReferenceExpression: x2 (OperationKind.LocalReferenceExpression, Type: T) (Syntax: 'x2')
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
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


