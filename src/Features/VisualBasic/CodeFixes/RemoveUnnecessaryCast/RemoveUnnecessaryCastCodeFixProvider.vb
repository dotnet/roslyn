' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryCast

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateEndConstruct)>
    Partial Friend Class RemoveUnnecessaryCastCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model = DirectCast(Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False), SemanticModel)

            Dim node = GetCastNode(root, model, span, cancellationToken)
            If node Is Nothing Then
                Return
            End If

            context.RegisterCodeFix(
                New MyCodeAction(
                    VBFeaturesResources.RemoveUnnecessaryCast,
                    Function(c) RemoveUnnecessaryCastAsync(document, node, c)),
                context.Diagnostics)
        End Function

        Private Shared Function GetCastNode(root As SyntaxNode, model As SemanticModel, span As TextSpan, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return Nothing
            End If

            Dim node = token.GetAncestors(Of ExpressionSyntax)() _
                            .Where(Function(c) TypeOf c Is CastExpressionSyntax OrElse TypeOf c Is PredefinedCastExpressionSyntax) _
                            .FirstOrDefault(Function(c) c.Span.IntersectsWith(span) AndAlso IsUnnecessaryCast(c, model, cancellationToken))
            Return node
        End Function

        Private Shared Function IsUnnecessaryCast(node As ExpressionSyntax, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim castExpression = TryCast(node, CastExpressionSyntax)
            If castExpression IsNot Nothing Then
                Return castExpression.IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
            End If

            Dim predefinedCastExpression = TryCast(node, PredefinedCastExpressionSyntax)
            If predefinedCastExpression IsNot Nothing Then
                Return predefinedCastExpression.IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
            End If

            Return False
        End Function

        Private Shared Async Function RemoveUnnecessaryCastAsync(document As Document, node As ExpressionSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            ' First, annotate our expression so that we can get back to it.
            Dim updatedDocument = Await document.ReplaceNodeAsync(node, node.WithAdditionalAnnotations(s_expressionAnnotation), cancellationToken).ConfigureAwait(False)

            Dim expression = Await FindNodeWithAnnotationAsync(Of ExpressionSyntax)(s_expressionAnnotation, updatedDocument, cancellationToken).ConfigureAwait(False)

            ' Next, make the parenting statement of the expression semantically explicit
            Dim parentStatement = expression.FirstAncestorOrSelf(Of StatementSyntax)()
            Dim explicitParentStatement = Await Simplifier.ExpandAsync(parentStatement, updatedDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)
            explicitParentStatement = explicitParentStatement.WithAdditionalAnnotations(Formatter.Annotation, s_statementAnnotation)

            updatedDocument = Await updatedDocument.ReplaceNodeAsync(parentStatement, explicitParentStatement, cancellationToken).ConfigureAwait(False)

            ' Next, make the statement after the parenting statement of the expression semantically explicit.
            parentStatement = Await FindNodeWithAnnotationAsync(Of StatementSyntax)(s_statementAnnotation, updatedDocument, cancellationToken).ConfigureAwait(False)
            Dim nextStatement = parentStatement.GetNextStatement()
            If nextStatement IsNot Nothing Then
                Dim explicitNextStatement = Await Simplifier.ExpandAsync(nextStatement, updatedDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)
                updatedDocument = Await updatedDocument.ReplaceNodeAsync(nextStatement, explicitNextStatement, cancellationToken).ConfigureAwait(False)
            End If

            updatedDocument = Await RewriteCoreAsync(updatedDocument, expression, cancellationToken).ConfigureAwait(False)

            ' Remove added _expressionAnnotation and _statementAnnotation.
            updatedDocument = Await RemoveNodesAndTokensWithAnnotationAsync(s_expressionAnnotation, updatedDocument, cancellationToken).ConfigureAwait(False)
            updatedDocument = Await RemoveNodesAndTokensWithAnnotationAsync(s_statementAnnotation, updatedDocument, cancellationToken).ConfigureAwait(False)

            Return updatedDocument
        End Function

        Private Shared Async Function RemoveNodesAndTokensWithAnnotationAsync(annotation As SyntaxAnnotation, document As Document, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim nodesWithAnnotation = Await FindNodesWithAnnotationAsync(annotation, document, cancellationToken).ConfigureAwait(False)
            root = root.ReplaceSyntax(
                nodesWithAnnotation.Where(Function(n) n.IsNode).Select(Function(n) n.AsNode),
                Function(o, n) o.WithoutAnnotations(annotation),
                nodesWithAnnotation.Where(Function(n) n.IsToken).Select(Function(n) n.AsToken),
                Function(o, n) o.WithoutAnnotations(annotation),
                SpecializedCollections.EmptyEnumerable(Of SyntaxTrivia),
                Nothing)
            Return document.WithSyntaxRoot(root)
        End Function

        Private Shared Async Function RewriteCoreAsync(document As Document, originalExpr As ExpressionSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            ' Finally, rewrite the cast expression
            Dim exprToRewrite As ExpressionSyntax = Nothing
            Dim annotatedNodes = Await FindNodesWithAnnotationAsync(s_expressionAnnotation, document, cancellationToken).ConfigureAwait(False)

            For Each annotatedNode In annotatedNodes
                exprToRewrite = TryCast(annotatedNode.AsNode, ExpressionSyntax)
                If exprToRewrite IsNot Nothing AndAlso exprToRewrite.IsKind(originalExpr.Kind) Then
                    If annotatedNodes.Count > 1 Then
                        ' Ensure cast is unnecessary
                        Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                        If IsUnnecessaryCast(exprToRewrite, model, cancellationToken) Then
                            Exit For
                        End If
                    Else
                        Exit For
                    End If
                End If
                exprToRewrite = Nothing
            Next

            If exprToRewrite Is Nothing Then
                Return document
            End If

            Dim rewriter = New Rewriter(exprToRewrite)
            Dim newExpression = rewriter.Visit(exprToRewrite)

            ' Remove the annotation from the expression so that it isn't hanging around later.
            If newExpression.HasAnnotation(s_expressionAnnotation) Then

                newExpression = newExpression.WithoutAnnotations(s_expressionAnnotation)

            ElseIf newExpression.IsKind(SyntaxKind.ParenthesizedExpression) Then

                Dim parenthesizedExpression = DirectCast(newExpression, ParenthesizedExpressionSyntax)
                If parenthesizedExpression.Expression.HasAnnotation(s_expressionAnnotation) Then
                    newExpression = parenthesizedExpression _
                        .WithExpression(parenthesizedExpression.Expression.WithoutAnnotations(s_expressionAnnotation))
                End If

            End If

            document = Await document.ReplaceNodeAsync(exprToRewrite, newExpression, cancellationToken).ConfigureAwait(False)

            If annotatedNodes.Count > 1 Then
                Return Await RewriteCoreAsync(document, originalExpr, cancellationToken).ConfigureAwait(False)
            End If

            Return document
        End Function

        Private Shared ReadOnly s_expressionAnnotation As New SyntaxAnnotation
        Private Shared ReadOnly s_statementAnnotation As New SyntaxAnnotation

        Private Shared Async Function FindNodeWithAnnotationAsync(Of T As SyntaxNode)(annotation As SyntaxAnnotation, document As Document, cancellationToken As CancellationToken) As Task(Of T)
            Dim annotatedNodes = Await FindNodesWithAnnotationAsync(annotation, document, cancellationToken).ConfigureAwait(False)
            Dim result = annotatedNodes.Single().AsNode()
            Return DirectCast(result, T)
        End Function

        Private Shared Async Function FindNodesWithAnnotationAsync(annotation As SyntaxAnnotation, document As Document, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of SyntaxNodeOrToken))
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Return root.GetAnnotatedNodesAndTokens(annotation)
        End Function

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return RemoveUnnecessaryCastFixAllProvider.Instance
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
