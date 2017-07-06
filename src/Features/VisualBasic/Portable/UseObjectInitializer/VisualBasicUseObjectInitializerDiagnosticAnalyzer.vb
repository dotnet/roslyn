' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseObjectInitializerDiagnosticAnalyzer
        Inherits AbstractUseObjectInitializerDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides ReadOnly Property FadeOutOperatorToken As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function AreObjectInitializersSupported(context As SyntaxNodeAnalysisContext) As Boolean
            'Object Initializers are supported in all the versions of Visual Basic we support
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
