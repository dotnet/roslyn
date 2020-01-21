' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary
    Partial Friend Class InlineTemporaryCodeRefactoringProvider
        Partial Private Class ReferenceRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _definition As ModifiedIdentifierSyntax
            Private ReadOnly _expressionToInline As ExpressionSyntax
            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _localSymbol As ILocalSymbol

            Public Sub New(
                semanticModel As SemanticModel,
                modifiedIdentifier As ModifiedIdentifierSyntax,
                expressionToInline As ExpressionSyntax,
                cancellationToken As CancellationToken
            )

                _definition = modifiedIdentifier
                _semanticModel = semanticModel
                _expressionToInline = expressionToInline
                _cancellationToken = cancellationToken
                _localSymbol = DirectCast(_semanticModel.GetDeclaredSymbol(_definition), ILocalSymbol)
            End Sub

            Private Function IsReference(node As SimpleNameSyntax) As Boolean
                If Not CaseInsensitiveComparison.Equals(node.Identifier.ValueText, _definition.Identifier.ValueText) Then
                    Return False
                End If

                Dim symbolInfo = _semanticModel.GetSymbolInfo(node)
                Return Equals(symbolInfo.Symbol, _localSymbol)
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                If IsReference(node) Then
                    If HasConflict(node, _definition, _expressionToInline, _semanticModel) Then
                        Return node.Update(node.Identifier.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(VBFeaturesResources.Conflict_s_detected)))
                    End If

                    ' Make sure we attach any trailing trivia from the identifier node we're replacing
                    ' to the new expression so that we don't remove any line continuation characters.
                    Return _expressionToInline _
                        .Parenthesize() _
                        .WithTrailingTrivia(node.GetTrailingTrivia()) _
                        .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
                End If

                Return MyBase.VisitIdentifierName(node)
            End Function

            Public Overrides Function VisitNameColonEquals(node As NameColonEqualsSyntax) As SyntaxNode
                If node.IsParentKind(SyntaxKind.SimpleArgument) AndAlso
                    node.Parent.IsParentKind(SyntaxKind.TupleExpression) Then

                    ' Temporaries should not be inlined in the name portion of a named tuple element
                    ' This special case should be removed once https://github.com/dotnet/roslyn/issues/16697 is fixed
                    Return node
                End If

                Return MyBase.VisitNameColonEquals(node)
            End Function

            Public Overloads Shared Function Visit(
                semanticModel As SemanticModel,
                scope As SyntaxNode,
                modifiedIdentifier As ModifiedIdentifierSyntax,
                expressionToInline As ExpressionSyntax,
                cancellationToken As CancellationToken
            ) As SyntaxNode

                Dim rewriter = New ReferenceRewriter(semanticModel, modifiedIdentifier, expressionToInline, cancellationToken)
                Return rewriter.Visit(scope)
            End Function

        End Class
    End Class
End Namespace
