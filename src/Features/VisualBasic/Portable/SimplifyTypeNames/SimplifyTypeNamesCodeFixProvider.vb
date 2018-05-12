' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyNames), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits AbstractSimplifyTypeNamesCodeFixProvider

        Protected Overrides Function GetTitle(simplifyDiagnosticId As String, nodeText As String) As String
            Select Case simplifyDiagnosticId
                Case IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                     IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId
                    Return String.Format(VBFeaturesResources.Simplify_name_0, nodeText)

                Case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                     IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId
                    Return String.Format(VBFeaturesResources.Simplify_member_access_0, nodeText)

                Case IDEDiagnosticIds.RemoveQualificationDiagnosticId
                    Return VBFeaturesResources.Remove_Me_qualification

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(simplifyDiagnosticId)
            End Select
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Return VisualBasicSimplifyTypeNamesDiagnosticAnalyzer.IsCandidate(node)
        End Function

        Protected Overrides Function CanSimplifyTypeNameExpression(model As SemanticModel, node As SyntaxNode, optionSet As OptionSet, ByRef issueSpan As TextSpan, ByRef diagnosticId As String, cancellationToken As CancellationToken) As Boolean
            Return VisualBasicSimplifyTypeNamesDiagnosticAnalyzer.CanSimplifyTypeNameExpression(model, node, optionSet, issueSpan, diagnosticId, cancellationToken)
        End Function

        Protected Overrides Async Function SimplifyTypeNameAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim expression = node
            Dim annotatedExpression = expression.WithAdditionalAnnotations(Simplifier.Annotation)

            Dim newRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            newRoot = newRoot.ReplaceNode(expression, annotatedExpression)

            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace
