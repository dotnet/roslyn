' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    Friend MustInherit Class AbstractIntrinsicOperatorSignatureHelpProvider(Of TSyntaxNode As SyntaxNode)
        Inherits AbstractVisualBasicSignatureHelpProvider

        Protected MustOverride Function IsTriggerToken(token As SyntaxToken) As Boolean
        Protected MustOverride Function IsArgumentListToken(node As TSyntaxNode, token As SyntaxToken) As Boolean
        Protected MustOverride Function GetIntrinsicOperatorDocumentationAsync(node As TSyntaxNode, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))

        Private Function TryGetSyntaxNode(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef node As TSyntaxNode) As Boolean
            Return CommonSignatureHelpUtilities.TryGetSyntax(
                root,
                position,
                syntaxFacts,
                triggerReason,
                AddressOf IsTriggerToken,
                AddressOf IsArgumentListToken,
                cancellationToken,
                node)
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As MemberDisplayOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim node As TSyntaxNode = Nothing
            If Not TryGetSyntaxNode(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, node) Then
                Return Nothing
            End If

            Dim items As New List(Of SignatureHelpItem)

            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(node, cancellationToken).ConfigureAwait(False)
            For Each documentation In Await GetIntrinsicOperatorDocumentationAsync(node, document, cancellationToken).ConfigureAwait(False)
                Dim signatureHelpItem = GetSignatureHelpItemForIntrinsicOperator(document, semanticModel, node.SpanStart, documentation, cancellationToken)
                items.Add(signatureHelpItem)
            Next

            Dim textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(node, node.SpanStart, Function(n) n.ChildTokens.FirstOrDefault(Function(c) c.Kind = SyntaxKind.CloseParenToken))
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Return CreateSignatureHelpItems(
                items, textSpan,
                GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex:=Nothing, parameterIndexOverride:=-1)
        End Function

        Friend Shared Function GetSignatureHelpItemForIntrinsicOperator(document As Document, semanticModel As SemanticModel, position As Integer, documentation As AbstractIntrinsicOperatorDocumentation, cancellationToken As CancellationToken) As SignatureHelpItem
            Dim parameters As New List(Of SignatureHelpSymbolParameter)

            For i = 0 To documentation.ParameterCount - 1
                Dim capturedIndex = i
                parameters.Add(
                    New SignatureHelpSymbolParameter(
                        name:=documentation.GetParameterName(i),
                        isOptional:=False,
                        documentationFactory:=Function(c As CancellationToken) documentation.GetParameterDocumentation(capturedIndex).ToSymbolDisplayParts().ToTaggedText(),
                        displayParts:=documentation.GetParameterDisplayParts(i)))
            Next

            Dim suffixParts = documentation.GetSuffix(semanticModel, position, Nothing, cancellationToken)

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()

            Return CreateItem(
                Nothing, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic:=False,
                documentationFactory:=Function(c) SpecializedCollections.SingletonEnumerable(New TaggedText(TextTags.Text, documentation.DocumentationText)),
                prefixParts:=documentation.PrefixParts,
                separatorParts:=GetSeparatorParts(),
                suffixParts:=suffixParts,
                parameters:=parameters)
        End Function

        Protected Overridable Function GetCurrentArgumentStateWorker(node As SyntaxNode, position As Integer) As SignatureHelpState
            Dim commaTokens As New List(Of SyntaxToken)
            commaTokens.AddRange(node.ChildTokens().Where(Function(token) token.Kind = SyntaxKind.CommaToken))

            ' Also get any leading skipped tokens on the next token after this node
            Dim nextToken = node.GetLastToken().GetNextToken()

            For Each leadingTrivia In nextToken.LeadingTrivia
                If leadingTrivia.Kind = SyntaxKind.SkippedTokensTrivia Then
                    commaTokens.AddRange(leadingTrivia.GetStructure().ChildTokens().Where(Function(token) token.Kind = SyntaxKind.CommaToken))
                End If
            Next

            ' Count how many commas are before us
            Return New SignatureHelpState(
                argumentIndex:=commaTokens.Where(Function(token) token.SpanStart < position).Count(),
                argumentCount:=commaTokens.Count() + 1,
                argumentName:=Nothing, argumentNames:=Nothing)
        End Function

        Private Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim node As TSyntaxNode = Nothing
            If TryGetSyntaxNode(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, node) AndAlso
                currentSpan.Start = node.SpanStart Then

                Return GetCurrentArgumentStateWorker(node, position)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
