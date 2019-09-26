' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider(NameOf(CollectionInitializerSignatureHelpProvider), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class CollectionInitializerSignatureHelpProvider
        Inherits AbstractOrdinaryMethodSignatureHelpProvider

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "{"c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = "}"c
        End Function

        Private Function TryGetInitializerExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, <Out> ByRef expression As CollectionInitializerSyntax) As Boolean
            Return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsInitializerExpressionToken, cancellationToken, expression) AndAlso
                   expression IsNot Nothing
        End Function

        Private Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return Not token.IsKind(SyntaxKind.None) AndAlso
               token.ValueText.Length = 1 AndAlso
               IsTriggerCharacter(token.ValueText(0)) AndAlso
               TypeOf token.Parent Is CollectionInitializerSyntax
        End Function

        Private Shared Function IsInitializerExpressionToken(expression As CollectionInitializerSyntax, token As SyntaxToken) As Boolean
            Return expression.Span.Contains(token.SpanStart) AndAlso token <> expression.CloseBraceToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim collectionInitializer As CollectionInitializerSyntax = Nothing
            If Not TryGetInitializerExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService)(), triggerInfo.TriggerReason, cancellationToken, collectionInitializer) Then
                Return Nothing
            End If

            Dim addMethods = Await CommonSignatureHelpUtilities.GetCollectionInitializerAddMethodsAsync(
                document, collectionInitializer.Parent, cancellationToken).ConfigureAwait(False)
            If addMethods.IsDefaultOrEmpty Then
                Return Nothing
            End If

            Dim textSpan = GetSignatureHelpSpan(collectionInitializer)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return CreateSignatureHelpItems(
                addMethods.Select(Function(s) ConvertMemberGroupMember(document, s, collectionInitializer.OpenBraceToken.SpanStart, semanticModel, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem:=Nothing)
        End Function

        Public Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As CollectionInitializerSyntax = Nothing
            If TryGetInitializerExpression(
                        root,
                        position,
                        syntaxFacts,
                        SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                        cancellationToken,
                        expression) AndAlso
                    currentSpan.Start = GetSignatureHelpSpan(expression).Start Then
                Return GetSignatureHelpState(expression, position)
            End If

            Return Nothing
        End Function
    End Class

End Namespace
