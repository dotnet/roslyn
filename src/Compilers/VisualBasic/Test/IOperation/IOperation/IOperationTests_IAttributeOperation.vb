' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    ' PROTOTYPE: Port more tests from C#.
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
    End Class
End Namespace
