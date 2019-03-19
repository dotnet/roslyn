' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class TypeImportCompletionProvider
        Inherits AbstractTypeImportCompletionProvider

        Protected Overrides Async Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetNamespacesInScope(semanticModel As SemanticModel, location As SyntaxNode, cancellationToken As CancellationToken) As HashSet(Of INamespaceSymbol)

            ' TODO: handle command line option -imports
            Dim result = New HashSet(Of INamespaceSymbol)(semanticModel.GetImportNamespacesInScope(location))
            Dim containingNamespaceDeclaration = location.GetAncestorOrThis(Of NamespaceStatementSyntax)()

            Dim containingNamespaceSymbol As INamespaceSymbol
            If containingNamespaceDeclaration Is Nothing Then
                containingNamespaceSymbol = semanticModel.Compilation.RootNamespace()
            Else
                containingNamespaceSymbol = semanticModel.GetDeclaredSymbol(containingNamespaceDeclaration, cancellationToken)
            End If

            If containingNamespaceSymbol IsNot Nothing Then
                While Not containingNamespaceSymbol.IsGlobalNamespace
                    result.Add(containingNamespaceSymbol)
                    containingNamespaceSymbol = containingNamespaceSymbol.ContainingNamespace
                End While
            End If

            Return result
        End Function

        Protected Overrides Async Function IsSemanticTriggerCharacterAsync(document As Document, characterPosition As Integer, cancellationToken As CancellationToken) As Task(Of Boolean)
            If document Is Nothing Then
                Return False
            End If

            Dim result = Await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(False)
            If result.HasValue Then
                Return result.Value
            End If

            Return True
        End Function
        Private Async Function IsTriggerOnDotAsync(document As Document, characterPosition As Integer, cancellationToken As CancellationToken) As Task(Of Boolean?)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            If text(characterPosition) <> "."c Then
                Return Nothing
            End If

            ' don't want to trigger after a number.  All other cases after dot are ok.
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(characterPosition)
            Return IsValidTriggerToken(token)
        End Function

        Private Function IsValidTriggerToken(token As SyntaxToken) As Boolean
            If token.Kind <> SyntaxKind.DotToken Then
                Return False
            End If

            Dim previousToken = token.GetPreviousToken()
            If previousToken.Kind = SyntaxKind.IntegerLiteralToken Then
                Return token.Parent.Kind <> SyntaxKind.SimpleMemberAccessExpression OrElse Not DirectCast(token.Parent, MemberAccessExpressionSyntax).Expression.IsKind(SyntaxKind.NumericLiteralExpression)
            End If

            Return True
        End Function
    End Class
End Namespace
