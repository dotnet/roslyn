' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp.Providers
    Friend MustInherit Class AbstractIntrinsicOperatorSignatureHelpProvider(Of TSyntaxNode As SyntaxNode)
        Inherits AbstractVisualBasicSignatureHelpProvider

        Protected MustOverride Function IsTriggerToken(token As SyntaxToken) As Boolean
        Protected MustOverride Function IsArgumentListToken(node As TSyntaxNode, token As SyntaxToken) As Boolean
        Protected MustOverride Function GetIntrinsicOperatorDocumentation(node As TSyntaxNode, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)

        Private Function TryGetSyntaxNode(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerKind, cancellationToken As CancellationToken, ByRef node As TSyntaxNode) As Boolean
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

        Protected Overrides Async Function ProvideSignaturesWorkerAsync(context As SignatureContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim trigger = context.Trigger
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim node As TSyntaxNode = Nothing
            If Not TryGetSyntaxNode(root, position, document.GetLanguageService(Of ISyntaxFactsService), trigger.Kind, cancellationToken, node) Then
                Return
            End If

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(node, cancellationToken).ConfigureAwait(False)
            For Each documentation In GetIntrinsicOperatorDocumentation(node, document, cancellationToken)
                Dim signatureHelpItem = GetSignatureHelpItemForIntrinsicOperator(document, semanticModel, node.SpanStart, documentation, cancellationToken)
                context.AddItem(signatureHelpItem)
            Next

            Dim textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(node, node.SpanStart, Function(n) n.ChildTokens.FirstOrDefault(Function(c) c.Kind = SyntaxKind.CloseParenToken))
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            context.SetSpan(textSpan)
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken))
        End Function

        Private Const DocumentationProperty = "Documentation"

        Friend Function GetSignatureHelpItemForIntrinsicOperator(document As Document, semanticModel As SemanticModel, position As Integer, documentation As AbstractIntrinsicOperatorDocumentation, cancellationToken As CancellationToken) As SignatureHelpItem
            Dim parameters As New List(Of CommonParameterData)

            For i = 0 To documentation.ParameterCount - 1
                Dim capturedIndex = i
                parameters.Add(
                    New CommonParameterData(
                        name:=documentation.GetParameterName(i),
                        isOptional:=False,
                        symbol:=Nothing,
                        position:=position,
                        displayParts:=documentation.GetParameterDisplayParts(i).ToImmutableArrayOrEmpty(),
                        properties:=ImmutableDictionary(Of String, String).Empty.Add(DocumentationProperty, documentation.GetParameterDocumentation(capturedIndex))))
            Next

            Dim suffixParts = documentation.GetSuffix(semanticModel, position, Nothing, cancellationToken)

            Dim symbolDisplayService = document.Project.LanguageServices.GetService(Of ISymbolDisplayService)()
            Dim anonymousTypeDisplayService = document.Project.LanguageServices.GetService(Of IAnonymousTypeDisplayService)()

            Dim item = CreateItem(
                Nothing, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic:=False,
                prefixParts:=documentation.PrefixParts,
                separatorParts:=GetSeparatorParts(),
                suffixParts:=suffixParts,
                parameters:=parameters)

            Return WithDocumentation(item, documentation)
        End Function

        Protected Shared Function WithDocumentation(item As SignatureHelpItem, documentation As AbstractIntrinsicOperatorDocumentation) As SignatureHelpItem
            Return item.WithProperties(item.Properties.SetItem(DocumentationProperty, documentation.DocumentationText))
        End Function

        Protected Shared Function GetDocumentation(item As SignatureHelpItem) As ImmutableArray(Of TaggedText)
            Dim documentation As String = Nothing
            If item.Properties.TryGetValue(DocumentationProperty, documentation) Then
                Return ImmutableArray.Create(New TaggedText(TextTags.Text, documentation))
            Else
                Return ImmutableArray(Of TaggedText).Empty
            End If
        End Function

        Protected Shared Function GetDocumentation(parameter As SignatureHelpParameter) As ImmutableArray(Of TaggedText)
            Dim documentation As String = Nothing
            If parameter.Properties.TryGetValue(DocumentationProperty, documentation) Then
                Return ImmutableArray.Create(New TaggedText(TextTags.Text, documentation))
            Else
                Return ImmutableArray(Of TaggedText).Empty
            End If
        End Function

        Public Overrides Function GetItemDocumentationAsync(document As Document, item As SignatureHelpItem, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TaggedText))
            Return Task.FromResult(GetDocumentation(item))
        End Function

        Public Overrides Function GetParameterDocumentationAsync(document As Document, parameter As SignatureHelpParameter, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TaggedText))
            Return Task.FromResult(GetDocumentation(parameter))
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

        Protected Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim node As TSyntaxNode = Nothing
            If TryGetSyntaxNode(root, position, syntaxFacts, SignatureHelpTriggerKind.Other, cancellationToken, node) AndAlso
                currentSpan.Start = node.SpanStart Then

                Return GetCurrentArgumentStateWorker(node, position)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
