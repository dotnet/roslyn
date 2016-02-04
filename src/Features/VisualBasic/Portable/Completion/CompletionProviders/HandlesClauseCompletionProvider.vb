' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class HandlesClauseCompletionProvider
        Inherits AbstractSymbolCompletionProvider

        Protected Overrides Function GetSymbolsWorker(context As AbstractSyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol))
            Dim vbContext = DirectCast(context, VisualBasicSyntaxContext)

            If context.SyntaxTree.IsInNonUserCode(position, cancellationToken) OrElse
                context.SyntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            If context.TargetToken.Kind = SyntaxKind.None Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            ' Handles or a comma
            If context.TargetToken.IsChildToken(Of HandlesClauseSyntax)(Function(hc) hc.HandlesKeyword) OrElse
                context.TargetToken.IsChildSeparatorToken(Function(hc As HandlesClauseSyntax) hc.Events) Then
                Return Task.FromResult(GetTopLevelIdentifiersAsync(vbContext, context.TargetToken, cancellationToken))
            End If

            ' Handles x. or , x.
            If context.TargetToken.IsChildToken(Of HandlesClauseItemSyntax)(Function(hc) hc.DotToken) Then
                Return Task.FromResult(LookUpEventsAsync(vbContext, context.TargetToken, cancellationToken))
            End If

            Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Private Function GetTopLevelIdentifiersAsync(
            context As VisualBasicSyntaxContext,
            token As SyntaxToken,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken)
            Dim containingType = TryCast(containingSymbol, ITypeSymbol)
            If containingType Is Nothing Then
                ' We got the containing method as our enclosing type.
                containingType = containingSymbol.ContainingType
            End If

            If containingType Is Nothing Then
                ' We've somehow failed to find a containing type.
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            ' Instance or shared variables declared WithEvents
            Dim symbols = context.SemanticModel.LookupSymbols(context.Position, DirectCast(containingType, INamespaceOrTypeSymbol), includeReducedExtensionMethods:=True)
            Return symbols.Where(Function(s) IsWithEvents(s))
        End Function

        Private Function LookUpEventsAsync(
            context As VisualBasicSyntaxContext,
            token As SyntaxToken,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            ' We came up on a dot, so the previous token will tell us in which object we should find events.
            Dim containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken)
            Dim containingType = TryCast(containingSymbol, ITypeSymbol)
            If containingType Is Nothing Then
                ' We got the containing method as our enclosing type.
                containingType = containingSymbol.ContainingType
            End If

            If containingType Is Nothing Then
                ' We've somehow failed to find a containing type.
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            Dim result As IEnumerable(Of ISymbol) = Nothing

            Dim previousToken = token.GetPreviousToken()
            Select Case previousToken.Kind
                Case SyntaxKind.MeKeyword, SyntaxKind.MyClassKeyword
                    result = context.SemanticModel.LookupSymbols(context.Position, containingType).OfType(Of IEventSymbol)()
                Case SyntaxKind.MyBaseKeyword
                    result = context.SemanticModel.LookupSymbols(context.Position, containingType.BaseType).OfType(Of IEventSymbol)()
                Case SyntaxKind.IdentifierToken
                    ' We must be looking at a WithEvents property.
                    Dim symbolInfo = context.SemanticModel.GetSymbolInfo(previousToken, cancellationToken)
                    If symbolInfo.Symbol IsNot Nothing Then
                        Dim type = TryCast(symbolInfo.Symbol, IPropertySymbol)?.Type
                        If type IsNot Nothing Then
                            result = context.SemanticModel.LookupSymbols(token.SpanStart, type).OfType(Of IEventSymbol)()
                        End If
                    End If
            End Select

            Return result
        End Function

        Private Function CreateCompletionItem(position As Integer,
                                              symbol As ISymbol,
                                              span As TextSpan,
                                              context As VisualBasicSyntaxContext) As CompletionItem

            Dim displayAndInsertionText = CompletionUtilities.GetDisplayAndInsertionText(
                symbol, isAttributeNameContext:=False, isAfterDot:=context.IsRightOfNameSeparator, isWithinAsyncMethod:=context.WithinAsyncMethod, syntaxFacts:=context.GetLanguageService(Of ISyntaxFactsService)())

            Return New SymbolCompletionItem(Me,
                                            displayAndInsertionText.Item1,
                                            displayAndInsertionText.Item2,
                                            span,
                                            position,
                                            {symbol}.ToList(),
                                            context, rules:=ItemRules.Instance)
        End Function

        Private Function IsWithEvents(s As ISymbol) As Boolean
            Dim [property] = TryCast(s, IPropertySymbol)
            If [property] IsNot Nothing Then
                Return [property].IsWithEvents
            End If

            Return False
        End Function

        Protected Overrides Function GetDisplayAndInsertionText(symbol As ISymbol,
                                                                context As AbstractSyntaxContext) As ValueTuple(Of String, String)

            Return CompletionUtilities.GetDisplayAndInsertionText(symbol, context.IsAttributeNameContext, context.IsRightOfNameSeparator, isWithinAsyncMethod:=DirectCast(context, VisualBasicSyntaxContext).WithinAsyncMethod, syntaxFacts:=context.GetLanguageService(Of ISyntaxFactsService))
        End Function

        Protected Overrides Async Function CreateContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of AbstractSyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GetTextChangeSpan(text As SourceText, position As Integer) As TextSpan
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Protected Overrides Function GetCompletionItemRules() As CompletionItemRules
            Return ItemRules.Instance
        End Function
    End Class
End Namespace
