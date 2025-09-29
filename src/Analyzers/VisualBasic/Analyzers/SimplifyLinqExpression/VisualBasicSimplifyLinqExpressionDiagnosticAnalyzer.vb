' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.SimplifyLinqExpression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicSimplifyLinqExpressionDiagnosticAnalyzer
        Inherits AbstractSimplifyLinqExpressionDiagnosticAnalyzer(Of InvocationExpressionSyntax, MemberAccessExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides ReadOnly Property ConflictsWithMemberByNameOnly As Boolean = True

        Protected Overrides Function TryGetNextInvocationInChain(invocation As IInvocationOperation) As IInvocationOperation
            ' Unlike C# in VB exension methods are related in a simple child-parent relationship
            ' so in the case of A().ExensionB() to get from A to ExensionB we just need to get the parent of A
            Return TryCast(invocation.Parent, IInvocationOperation)
        End Function
    End Class
End Namespace
