' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub PropertyReferenceExpression_PropertyReferenceInDerivedTypeUsesDerivedTypeAsInstanceType()
            Dim source = <![CDATA[
Option Strict On
Module M1
    Sub Method1()
        Dim c2 As C2 = New C2 With {.P1 = New Object}'BIND:"P1"
    End Sub

    Class C1
        Public Overridable Property P1 As Object
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceExpression: Property M1.C1.P1 As System.Object (OperationKind.PropertyReferenceExpression, Type: System.Object) (Syntax: 'P1')
  Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: M1.C2) (Syntax: 'New C2 With ... New Object}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
