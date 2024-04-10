' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInstanceReferenceExpression_SimpleBaseReference()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Overridable Sub M1()
    End Sub
End Class

Public Class C2
    Inherits C1
    Public Overrides Sub M1()
        MyBase.M1()'BIND:"MyBase"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'MyBase')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of MyBaseExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IInstanceReferenceExpression_BaseNoMemberReference()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Overridable Sub M1()
        MyBase.M1()'BIND:"MyBase"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid) (Syntax: 'MyBase')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30456: 'M1' is not a member of 'Object'.
        MyBase.M1()'BIND:"MyBase"
        ~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of MyBaseExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
