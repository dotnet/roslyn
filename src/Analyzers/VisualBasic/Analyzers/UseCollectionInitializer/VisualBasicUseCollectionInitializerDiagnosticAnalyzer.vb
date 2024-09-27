' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
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
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            ObjectCollectionInitializerSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetAnalyzer() As VisualBasicCollectionInitializerAnalyzer
            Return VisualBasicCollectionInitializerAnalyzer.Allocate()
        End Function

        Protected Overrides Function AreCollectionInitializersSupported(compilation As Compilation) As Boolean
            Return True
        End Function

        Protected Overrides Function AreCollectionExpressionsSupported(compilation As Compilation) As Boolean
            Return False
        End Function

        Protected Overrides Function CanUseCollectionExpression(semanticModel As SemanticModel, objectCreationExpression As ObjectCreationExpressionSyntax, expressionType As INamedTypeSymbol, allowSemanticsChange As Boolean, cancellationToken As CancellationToken, ByRef changesSemantics As Boolean) As Boolean
            Throw ExceptionUtilities.Unreachable()
        End Function
    End Class
End Namespace
