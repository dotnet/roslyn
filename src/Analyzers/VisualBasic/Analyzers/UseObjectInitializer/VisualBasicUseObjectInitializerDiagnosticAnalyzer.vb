' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUseObjectInitializerDiagnosticAnalyzer
        Inherits AbstractUseObjectInitializerDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            ObjectMemberInitializerSyntax,
            VisualBasicUseNamedMemberInitializerAnalyzer)

        Protected Overrides ReadOnly Property FadeOutOperatorToken As Boolean = False

        Protected Overrides Function AreObjectInitializersSupported(compilation As Compilation) As Boolean
            'Object Initializers are supported in all the versions of Visual Basic we support
            Return True
        End Function

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function IsValidContainingStatement(node As StatementSyntax) As Boolean
            Return True
        End Function

        Protected Overrides Function GetAnalyzer() As VisualBasicUseNamedMemberInitializerAnalyzer
            Return VisualBasicUseNamedMemberInitializerAnalyzer.Allocate()
        End Function
    End Class
End Namespace
