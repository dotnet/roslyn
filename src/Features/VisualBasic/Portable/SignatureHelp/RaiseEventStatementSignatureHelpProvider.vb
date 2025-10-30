' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("RaiseEventSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Friend Class RaiseEventStatementSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create("("c, ","c)

        Public Overrides ReadOnly Property RetriggerCharacters As ImmutableArray(Of Char) = ImmutableArray(Of Char).Empty

        Private Shared Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState?
            Dim statement As RaiseEventStatementSyntax = Nothing
            If TryGetRaiseEventStatement(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, statement) AndAlso
                currentSpan.Start = statement.Name.SpanStart Then

                Return SignatureHelpUtilities.GetSignatureHelpState(statement.ArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Shared Function TryGetRaiseEventStatement(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef statement As RaiseEventStatementSyntax) As Boolean
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
            options As MemberDisplayOptions,
            cancellationToken As CancellationToken
        ) As Task(Of SignatureHelpItems)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim raiseEventStatement As RaiseEventStatementSyntax = Nothing
            If Not TryGetRaiseEventStatement(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, raiseEventStatement) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim containingType = semanticModel.GetEnclosingSymbol(position, cancellationToken).ContainingType

            Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)()

            Dim events = If(syntaxFactsService.IsInStaticContext(raiseEventStatement),
                semanticModel.LookupStaticMembers(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText),
                semanticModel.LookupSymbols(raiseEventStatement.SpanStart, containingType, raiseEventStatement.Name.Identifier.ValueText))

            Dim allowedEvents = events.WhereAsArray(Function(s) s.Kind = SymbolKind.Event AndAlso Equals(s.ContainingType, containingType)).
                                       OfType(Of IEventSymbol)().
                                       ToImmutableArrayOrEmpty().
                                       FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation).
                                       Sort(semanticModel, raiseEventStatement.SpanStart)

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(raiseEventStatement.ArgumentList, raiseEventStatement.Name.SpanStart)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Return CreateSignatureHelpItems(
                allowedEvents.Select(Function(e) Convert(e, raiseEventStatement, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItemIndex:=Nothing, parameterIndexOverride:=-1)
        End Function

        Private Overloads Shared Function Convert(
            eventSymbol As IEventSymbol,
            raiseEventStatement As RaiseEventStatementSyntax,
            semanticModel As SemanticModel,
            structuralTypeDisplayService As IStructuralTypeDisplayService,
            documentationCommentFormattingService As IDocumentationCommentFormattingService
        ) As SignatureHelpItem

            Dim position = raiseEventStatement.SpanStart

            Dim type = DirectCast(eventSymbol.Type, INamedTypeSymbol)

            Dim item = CreateItem(
                eventSymbol, semanticModel, position,
                structuralTypeDisplayService,
                False,
                eventSymbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(eventSymbol, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(),
                type.DelegateInvokeMethod.GetParameters().Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList())

            Return item
        End Function

        Private Shared Function GetPreambleParts(
            eventSymbol As IEventSymbol,
            semanticModel As SemanticModel,
            position As Integer
        ) As IList(Of SymbolDisplayPart)

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

        Private Shared Function GetPostambleParts() As IList(Of SymbolDisplayPart)
            Return {Punctuation(SyntaxKind.CloseParenToken)}
        End Function
    End Class
End Namespace
