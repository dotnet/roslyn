' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Operations

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
    End Class
End Namespace
