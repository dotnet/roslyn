' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseCollectionExpression
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUseCollectionInitializerDiagnosticAnalyzer
        Inherits AbstractUseCollectionInitializerDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        ' VB's member-init fade historically highlighted only the receiver expression, not the
        ' `.` operator. Mirrors the legacy `VisualBasicUseObjectInitializerDiagnosticAnalyzer.FadeOutOperatorToken`.
        Protected Overrides ReadOnly Property FadeOutOperatorToken As Boolean = False

        Protected Overrides Function GetAnalyzer() As VisualBasicCollectionInitializerAnalyzer
            Return VisualBasicCollectionInitializerAnalyzer.Allocate()
        End Function

        Protected Overrides Function AreCollectionInitializersSupported(compilation As Compilation) As Boolean
            Return True
        End Function

        Protected Overrides Function AreCollectionExpressionsSupported(compilation As Compilation) As Boolean
            Return False
        End Function

        Protected Overrides Function HasExistingInvalidInitializerForCollectionExpression(objectCreationExpression As ObjectCreationExpressionSyntax) As Boolean
            ' VB has no collection-expression syntax, so the precondition this check guards is
            ' unreachable from VB. Returning false keeps the diagnostic-analyzer dispatch
            ' identical to legacy behavior; `AreCollectionExpressionsSupported = False` already
            ' blocks the collection-expression branch before this method would be called.
            Return False
        End Function

        Protected Overrides Function CanUseCollectionExpression(
                semanticModel As SemanticModel,
                objectCreationExpression As ObjectCreationExpressionSyntax,
                expressionType As INamedTypeSymbol,
                matches As ImmutableArray(Of InitializerMatch(Of SyntaxNode)),
                allowSemanticsChange As Boolean,
                cancellationToken As CancellationToken,
                ByRef changesSemantics As Boolean) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function IsValidContainingStatement(node As StatementSyntax) As Boolean
            Return True
        End Function
    End Class
End Namespace
