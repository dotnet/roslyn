' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Performance
    <ExportCodeFixProvider(CA1813DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic), [Shared]>
    Public Class CA1813BasicCodeFixProvider
        Inherits CA1813CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnosticId As String, cancellationToken As CancellationToken) As Task(Of Document)
            Dim attributeStatementSyntax = TryCast(nodeToFix, ClassStatementSyntax)
            If attributeStatementSyntax IsNot Nothing Then
                ' TODO : Organize the modifiers list after adding sealed modifier.
                Dim sealedModifier = SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword)
                Dim newAttributeStatementSyntax = attributeStatementSyntax.
                    WithModifiers(attributeStatementSyntax.Modifiers.Add(sealedModifier)).
                    WithAdditionalAnnotations(Formatter.Annotation)
                document = document.WithSyntaxRoot(root.ReplaceNode(attributeStatementSyntax, newAttributeStatementSyntax))
            End If

            Return Task.FromResult(document)
        End Function
    End Class
End Namespace