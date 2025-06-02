' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestCallerInfoImplicitCall()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class AAttribute
    Inherits Attribute
    Public Sub New(<CallerLineNumber> Optional lineNumber As Integer = -1)
        Console.WriteLine(lineNumber)
    End Sub
End Class

<A>'BIND:"A"
Class Test
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'A')
  IObjectCreationOperation (Constructor: Sub AAttribute..ctor([lineNumber As System.Int32 = -1])) (OperationKind.ObjectCreation, Type: AAttribute, IsImplicit) (Syntax: 'A')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: lineNumber) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'A')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12, IsImplicit) (Syntax: 'A')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestCallerMemberName_Class()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class AAttribute
    Inherits Attribute
    Public Sub New(<CallerMemberName> Optional callerName As String = "")
        Console.WriteLine(callerName)
    End Sub
End Class

<A>'BIND:"A"
Class Test
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'A')
  IObjectCreationOperation (Constructor: Sub AAttribute..ctor([callerName As System.String = ""])) (OperationKind.ObjectCreation, Type: AAttribute, IsImplicit) (Syntax: 'A')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: callerName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'A')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsImplicit) (Syntax: 'A')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestCallerMemberName_Method()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class AAttribute
    Inherits Attribute
    Public Sub New(<CallerMemberName> Optional callerName As String = "")
        Console.WriteLine(callerName)
    End Sub
End Class

Class Test
    <A>'BIND:"A"
    Public Sub M()
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'A')
  IObjectCreationOperation (Constructor: Sub AAttribute..ctor([callerName As System.String = ""])) (OperationKind.ObjectCreation, Type: AAttribute, IsImplicit) (Syntax: 'A')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: callerName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'A')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "M", IsImplicit) (Syntax: 'A')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestCallerMemberName_Parameter()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
    Public Sub New(<CallerMemberName> Optional callerName As String = "")
    End Sub
End Class

Class Test
    Public Sub M(<My> x As String)'BIND:"My"
    End Sub
End Class
]]>.Value
            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([callerName As System.String = ""])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: callerName) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "M", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestNonExistingAttribute()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<My>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My')
  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
    Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30002: Type 'My' is not defined.
<My>'BIND:"My"
 ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAttributeWithoutArguments()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
End Class

<My>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor()) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(0)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAttributeWithExplicitArgument()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
    Public Sub New(value As String)
    End Sub
End Class

<My("Value")>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My("Value")')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor(value As System.String)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My("Value")')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Value"')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Value") (Syntax: '"Value"')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAttributeWithExplicitArgument_IncorrectTypePassed()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
    Public Sub New(value As String)
    End Sub
End Class

<My(0)>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My(0)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor(value As System.String)) (OperationKind.ObjectCreation, Type: MyAttribute, IsInvalid, IsImplicit) (Syntax: 'My(0)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '0')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '0')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30934: Conversion from 'Integer' to 'String' cannot occur in a constant expression used as an argument to an attribute.
<My(0)>'BIND:"My"
    ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestAttributeWithExplicitArgumentOptionalParameter()
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional value As String = "")
    End Sub
End Class

<My("Value")>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My("Value")')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([value As System.String = ""])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My("Value")')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '"Value"')
            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Value") (Syntax: '"Value"')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Theory, CombinatorialData>
        Public Sub TestAttributeWithOptionalParameterNotPassed(withParentheses As Boolean)
            Dim attribute = If(withParentheses, "My()", "My")
            Dim attributeListSyntax = "<" + attribute + ">" + "'BIND:""My"""
            Dim source = <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional value As String = "")
    End Sub
End Class
]]>.Value + attributeListSyntax + <![CDATA[ 
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <expected>
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: '<%= attribute %>')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([value As System.String = ""])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: '<%= attribute %>')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: value) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
</expected>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub TestConversion()
            Dim source = <![CDATA[
Imports System

Class MyAttribute
    Inherits Attribute
    Public Sub New(x As Double)
    End Sub
End Class

<My(0.0D)>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(0.0D)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor(x As System.Double)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(0.0D)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '0.0D')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 0, IsImplicit) (Syntax: '0.0D')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
                ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 0.0) (Syntax: '0.0D')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
        null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub BadAttributeParameterType()
            Dim source = <![CDATA[
Imports System

Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Integer? = 0)
    End Sub
End Class

<My>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My')
  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
    Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30045: Attribute constructor has a parameter of type 'Integer?', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
<My>'BIND:"My"
 ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub BadAttributeParameterType2()
            Dim source = <![CDATA[
Imports System

Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Integer? = 0)
    End Sub
End Class

<My(Nothing)>'BIND:"My"
Class C
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My(Nothing)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([x As System.Nullable(Of System.Int32) = 0])) (OperationKind.ObjectCreation, Type: MyAttribute, IsInvalid, IsImplicit) (Syntax: 'My(Nothing)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30045: Attribute constructor has a parameter of type 'Integer?', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
<My(Nothing)>'BIND:"My"
 ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AttributeWithExplicitNullArgument()
            Dim source = <![CDATA[
Imports System

<My(Nothing)>'BIND:"My"
Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Type = Nothing)
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(Nothing)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([x As System.Type = Nothing])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(Nothing)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'Nothing')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Type, Constant: null, IsImplicit) (Syntax: 'Nothing')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AttributeWithDefaultNullArgument()
            Dim source = <![CDATA[
Imports System

<My>'BIND:"My"
Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Type = Nothing)
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([x As System.Type = Nothing])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'My')
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Type, Constant: null, IsImplicit) (Syntax: 'My')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsImplicit) (Syntax: 'My')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AttributeWithTypeOfArgument()
            Dim source = <![CDATA[
Imports System

<My(GetType(MyAttribute))>'BIND:"My"
Class MyAttribute
    Inherits Attribute
    Public Sub New(Optional x As Type = Nothing)
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(GetType(MyAttribute))')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor([x As System.Type = Nothing])) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(GetType(MyAttribute))')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'GetType(MyAttribute)')
          ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(MyAttribute)')
            TypeOperand: MyAttribute
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InvalidValue()
            Dim source = <![CDATA[
Imports System.Security.Permissions

<A>'BIND:"A"
Class A
    Inherits CodeAccessSecurityAttribute
    Public Sub New(Optional x As SecurityAction = 0)
        MyBase.New(x)
    End Sub
End Class
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'A')
  IObjectCreationOperation (Constructor: Sub A..ctor([x As System.Security.Permissions.SecurityAction = 0])) (OperationKind.ObjectCreation, Type: A, IsInvalid, IsImplicit) (Syntax: 'A')
    Arguments(1):
        IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'A')
          ILiteralOperation (OperationKind.Literal, Type: System.Security.Permissions.SecurityAction, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'A')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31214: SecurityAction value '0' is invalid for security attributes applied to a type or a method.
<A>'BIND:"A"
 ~
BC30610: Class 'A' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    SecurityAttribute: Public MustOverride Overloads Function CreatePermission() As IPermission.
Class A
      ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub InvalidAttributeParameterType()
            Dim source = <![CDATA[
Imports System

<My>'BIND:"My"
Class MyAttribute
    Inherits Attribute

    Public Sub New(ParamArray x As Integer()(,))
    End Sub
End Class

]]>.Value

            Dim expectedOperationTree = <![CDATA[
IAttributeOperation (OperationKind.Attribute, Type: null, IsInvalid) (Syntax: 'My')
  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'My')
    Children(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30045: Attribute constructor has a parameter of type 'Integer()(*,*)', which is not an integral, floating-point or Enum type or one of Object, Char, String, Boolean, System.Type or 1-dimensional array of these types.
<My>'BIND:"My"
 ~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Theory>
        <InlineData("Assembly")>
        <InlineData("Module")>
        Public Sub AssemblyAndModuleAttributeTargets(attributeTarget As String)
            Dim source = $"
Imports System

<{attributeTarget}: CLSCompliant(True)>'BIND:""{attributeTarget}: CLSCompliant(True)""
"
            Dim syntax As String
            If attributeTarget = "Assembly" Then
                syntax = "Assembly: C ... liant(True)"
            ElseIf attributeTarget = "Module" Then
                syntax = "Module: CLS ... liant(True)"
            Else
                Throw TestExceptionUtilities.UnexpectedValue(attributeTarget)
            End If

            Dim expectedOperationTree = $"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: '{syntax}')
  IObjectCreationOperation (Constructor: Sub System.CLSCompliantAttribute..ctor(isCompliant As System.Boolean)) (OperationKind.ObjectCreation, Type: System.CLSCompliantAttribute, IsImplicit) (Syntax: '{syntax}')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: isCompliant) (OperationKind.Argument, Type: null) (Syntax: 'True')
          ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ReturnAttributeTarget()
            Dim source = <![CDATA[
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(x As Integer)
    End Sub
End Class

Class C
    Public Function M() As <My(10)> String 'BIND:"My(10)"
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedOperationTree = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AttributeOnEnumMember()
            Dim source = <![CDATA[
Imports System

Class MyAttribute
    Inherits Attribute

    Public Sub New(x As Integer)
    End Sub
End Class

Enum E
    <My(10)>'BIND:"My(10)"
    A
End Enum
]]>.Value

            Dim expectedOperationTree = "
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'My(10)')
  IObjectCreationOperation (Constructor: Sub MyAttribute..ctor(x As System.Int32)) (OperationKind.ObjectCreation, Type: MyAttribute, IsImplicit) (Syntax: 'My(10)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '10')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
"

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
