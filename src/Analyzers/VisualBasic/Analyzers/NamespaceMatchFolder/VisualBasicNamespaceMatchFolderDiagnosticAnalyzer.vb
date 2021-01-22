' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Analyzers.NamespaceSync
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceMatchFolder
    Friend Class VisualBasicNamespaceMatchFolderDiagnosticAnalyzer
        Inherits AbstractNamespaceMatchFolderDiagnosticAnalyzer(Of NamespaceStatementSyntax)

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(syntaxContext As SyntaxNodeAnalysisContext) AnalyzeNamespaceNode(syntaxContext), SyntaxKind.NamespaceStatement)
        End Sub

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function GetNameSyntax(namespaceDeclaration As NamespaceStatementSyntax) As SyntaxNode
            Return namespaceDeclaration.Name
        End Function
    End Class

End Namespace
