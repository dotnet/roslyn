' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    <ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class ObjectCreationExpressionSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Private Shared Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As ObjectCreationExpressionSyntax = Nothing
            If TryGetObjectCreationExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, expression) AndAlso
                currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start Then

                Return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Shared Function TryGetObjectCreationExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef expression As ObjectCreationExpressionSyntax) As Boolean
            If Not CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsArgumentListToken, cancellationToken, expression) Then
                Return False
            End If

            Return expression.ArgumentList IsNot Nothing
        End Function

        Private Shared Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return (token.Kind = SyntaxKind.OpenParenToken OrElse token.Kind = SyntaxKind.CommaToken) AndAlso
                    TypeOf token.Parent Is ArgumentListSyntax AndAlso
                    TypeOf token.Parent.Parent Is ObjectCreationExpressionSyntax
        End Function

        Private Shared Function IsArgumentListToken(node As ObjectCreationExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.ArgumentList IsNot Nothing AndAlso
                node.ArgumentList.Span.Contains(token.SpanStart) AndAlso
                token <> node.ArgumentList.CloseParenToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As SignatureHelpOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim objectCreationExpression As ObjectCreationExpressionSyntax = Nothing
            If Not TryGetObjectCreationExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, objectCreationExpression) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim type = TryCast(semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type, INamedTypeSymbol)
            If type Is Nothing Then
                Return Nothing
            End If

            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return Nothing
            End If

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Dim itemsAndSelected = If(type.TypeKind = TypeKind.Delegate,
                GetDelegateTypeConstructors(objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService, type),
                GetNormalTypeConstructors(document, objectCreationExpression, semanticModel, structuralTypeDisplayService, type, within, options, cancellationToken))

            Return CreateSignatureHelpItems(itemsAndSelected.items,
                                            textSpan,
                                            GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken),
                                            itemsAndSelected.selectedItem)
        End Function
    End Class
End Namespace
