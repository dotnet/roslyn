' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Options
Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyNames), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                    IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                    IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId,
                    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId)
            End Get
        End Property

        Friend Shared Function GetNodeToSimplify(root As SyntaxNode, model As SemanticModel, span As TextSpan, optionSet As OptionSet, ByRef diagnosticId As String, cancellationToken As CancellationToken) As SyntaxNode
            diagnosticId = Nothing
            Dim token = root.FindToken(span.Start, findInsideTrivia:=True)

            If Not token.Span.IntersectsWith(span) Then
                Return Nothing
            End If

            Dim ancestors = token.GetAncestors(Of ExpressionSyntax)()
            For Each n In ancestors.Reverse
                If n.Span.IntersectsWith(span) AndAlso CanSimplifyTypeNameExpression(model, n, optionSet, span, diagnosticId, cancellationToken) Then
                    Return n
                End If
            Next

            Return Nothing
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim diagnosticId As String = Nothing
            Dim documentOptions = Await document.GetOptionsAsync(cancellationToken).ConfigureAwait(False)
            Dim node = GetNodeToSimplify(root, model, span, documentOptions, diagnosticId, cancellationToken)
            If node Is Nothing Then
                Return
            End If

            Dim title = GetTitle(diagnosticId, node.ConvertToSingleLine().ToString())
            context.RegisterCodeFix(New SimplifyTypeNameCodeAction(
                title, Function(c) SimplifyTypeNameAsync(document, node, c), diagnosticId),
                context.Diagnostics)
        End Function

        Private Shared Function GetTitle(simplifyDiagnosticId As String, nodeText As String) As String
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

        Private Shared Function CanSimplifyTypeNameExpression(model As SemanticModel, node As SyntaxNode, optionSet As OptionSet, span As TextSpan, ByRef diagnosticId As String, cancellationToken As CancellationToken) As Boolean
            Dim issueSpan As TextSpan
            If Not VisualBasicSimplifyTypeNamesDiagnosticAnalyzer.IsCandidate(node) OrElse
               Not VisualBasicSimplifyTypeNamesDiagnosticAnalyzer.CanSimplifyTypeNameExpression(model, node, optionSet, issueSpan, diagnosticId, cancellationToken) Then
                Return False
            End If

            Return issueSpan.Equals(span)
        End Function

        Private Async Function SimplifyTypeNameAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim expression = node
            Dim annotatedExpression = expression.WithAdditionalAnnotations(Simplifier.Annotation)

            Dim newRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            newRoot = newRoot.ReplaceNode(expression, annotatedExpression)

            Return document.WithSyntaxRoot(newRoot)
        End Function

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return SimplifyTypeNamesFixAllProvider.Instance
        End Function
    End Class
End Namespace
