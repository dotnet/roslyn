' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation), WorkItem(21769, "https://github.com/dotnet/roslyn/issues/21769")>
        <Fact()>
        Public Sub PropertyReferenceExpression_PropertyReferenceInWithDerivedTypeUsesDerivedTypeAsInstanceType_LValue()
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
IPropertyReferenceOperation: Property M1.C1.P1 As System.Object (OperationKind.PropertyReference, IsExpression, Type: System.Object) (Syntax: 'P1')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: M1.C2, IsImplicit) (Syntax: 'New C2 With ... New Object}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of IdentifierNameSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation), WorkItem(21769, "https://github.com/dotnet/roslyn/issues/21769")>
        <Fact()>
        Public Sub PropertyReferenceExpression_PropertyReferenceInWithDerivedTypeUsesDerivedTypeAsInstanceType_RValue()
            Dim source = <![CDATA[
Option Strict On
Module M1
    Sub Method1()
        Dim c2 As C2 = New C2 With {.P2 = .P1}'BIND:".P1"
        c2.P1 = Nothing
    End Sub

    Class C1
        Public Overridable Property P1 As Object
        Public Property P2 As Object
    End Class

    Class C2
        Inherits C1
    End Class
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IPropertyReferenceOperation: Property M1.C1.P1 As System.Object (OperationKind.PropertyReference, IsExpression, Type: System.Object) (Syntax: '.P1')
  Instance Receiver: 
    IInstanceReferenceOperation (OperationKind.InstanceReference, IsExpression, Type: M1.C2, IsImplicit) (Syntax: 'New C2 With {.P2 = .P1}')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MemberAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
