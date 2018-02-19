' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCollectionInitializerDiagnosticAnalyzer
        Inherits AbstractUseCollectionInitializerDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides Function AreCollectionInitializersSupported(context As SyntaxNodeAnalysisContext) As Boolean
            Return True
        End Function

        Protected Overrides Function GetObjectCreationSyntaxKind() As SyntaxKind
            Return SyntaxKind.ObjectCreationExpression
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function
    End Class
End Namespace
