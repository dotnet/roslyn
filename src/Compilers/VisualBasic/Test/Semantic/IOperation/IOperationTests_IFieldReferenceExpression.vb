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
        Public Sub FieldReference_Attribute()
            Dim source = <![CDATA[
Imports System.Diagnostics

Class C
    Private Const field As String = NameOf(field)

    <Conditional(field)>'BIND:"Conditional(field)"
    Private Sub M()
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IOperation:  (OperationKind.None, Type: null) (Syntax: 'Conditional(field)')
  Children(1):
      IFieldReferenceOperation: C.field As System.String (Static) (OperationKind.FieldReference, IsExpression, Type: System.String, Constant: "field") (Syntax: 'field')
        Instance Receiver: 
          null
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of AttributeSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
