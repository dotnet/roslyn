' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.SignatureHelp
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("RaiseEventSignatureHelpProvider", LanguageNames.VisualBasic)>
    Friend Class RaiseEventStatementSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return False
        End Function

        Public Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim statement As RaiseEventStatementSyntax = Nothing
            If TryGetRaiseEventStatement(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, statement) AndAlso
                currentSpan.Start = statement.Name.SpanStart Then

                Return SignatureHelpUtilities.GetSignatureHelpState(statement.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Function TryGetRaiseEventStatement(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef statement As RaiseEventStatementSyntax) As Boolean
            If Not CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsArgumentListToken, cancellationToken, statement) Then
                Return False
            End If

            Return statement.ArgumentList IsNot Nothing
        End Function

        Private Shared Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return (token.Kind = SyntaxKind.OpenParenToken OrElse token.Kind = SyntaxKind.CommaToken) AndAlso
                    TypeOf token.Parent Is ArgumentListSyntax AndAlso
                    TypeOf token.Parent.Parent Is RaiseEventStatementSyntax
        End Function

        Private Shared Function IsArgumentListToken(statement As RaiseEventStatementSyntax, token As SyntaxToken) As Boolean
            Return statement.ArgumentList IsNot Nothing AndAlso
                statement.ArgumentList.Span.Contains(token.SpanStart) AndAlso
                statement.ArgumentList.CloseParenToken <> token
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(
            document As Document,
            position As Integer,
            triggerInfo As SignatureHelpTriggerInfo,
            cancellationToken As CancellationToken
        ) As Task(Of SignatureHelpItems)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim raiseEventStatement As RaiseEventStatementSyntax = Nothing
            If Not TryGetRaiseEventStatement(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, raiseEventStatement) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim containingType = semanticModel.GetEnclosingSymbol(position, cancellationToken).ContainingType

            Dim syntaxFactsService = document.Project.LanguageServices.GetService(Of ISyntaxFactsService)()

            Dim events = If(syntaxFactsService.IsInStaticContext(raiseEventStatement),
                semanticModel.LookupStaticMembers(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText),
                semanticModel.LookupSymbols(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText))

            Dim symbolDisplayService = document.Project.LanguageServices.GetService(Of ISymbolDisplayService)()
            Dim allowedEvents = events.Where(Function(s) s.Kind = SymbolKind.Event AndAlso s.ContainingType Is containingType).
                                       Cast(Of IEventSymbol)().
                                       FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation).
                                       Sort(symbolDisplayService, semanticModel, raiseEventStatement.SpanStart)

            Dim anonymousTypeDisplayService = document.Project.LanguageServices.GetService(Of IAnonymousTypeDisplayService)()
            Dim documentationCommentFormattingService = document.Project.LanguageServices.GetService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(raiseEventStatement.ArgumentList, raiseEventStatement.Name.SpanStart)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Return CreateSignatureHelpItems(
                allowedEvents.Select(Function(e) Convert(e, raiseEventStatement, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken)),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken))
        End Function

        Private Overloads Function Convert(
            eventSymbol As IEventSymbol,
            raiseEventStatement As RaiseEventStatementSyntax,
            semanticModel As SemanticModel,
            symbolDisplayService As ISymbolDisplayService,
            anonymousTypeDisplayService As IAnonymousTypeDisplayService,
            documentationCommentFormattingService As IDocumentationCommentFormattingService,
            cancellationToken As CancellationToken
        ) As SignatureHelpItem

            Dim position = raiseEventStatement.SpanStart

            Dim type = DirectCast(eventSymbol.Type, INamedTypeSymbol)

            Dim item = CreateItem(
                eventSymbol, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                False,
                eventSymbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(eventSymbol, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(eventSymbol, semanticModel, position),
                type.DelegateInvokeMethod.GetParameters().Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)))

            Return item
        End Function

        Private Function GetPreambleParts(
            eventSymbol As IEventSymbol,
            semanticModel As SemanticModel,
            position As Integer
        ) As IEnumerable(Of SymbolDisplayPart)

            Dim result = New List(Of SymbolDisplayPart)()

            result.AddRange(eventSymbol.ContainingType.ToMinimalDisplayParts(semanticModel, position))
            result.Add(Punctuation(SyntaxKind.DotToken))

            Dim format = MinimallyQualifiedWithoutParametersFormat
            format = format.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType)
            format = format.RemoveKindOptions(SymbolDisplayKindOptions.IncludeMemberKeyword)

            result.AddRange(eventSymbol.ToMinimalDisplayParts(semanticModel, position, format))
            result.Add(Punctuation(SyntaxKind.OpenParenToken))

            Return result
        End Function

        Private Function GetPostambleParts(
            eventSymbol As IEventSymbol,
            semanticModel As SemanticModel,
            position As Integer
        ) As IEnumerable(Of SymbolDisplayPart)

            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace
