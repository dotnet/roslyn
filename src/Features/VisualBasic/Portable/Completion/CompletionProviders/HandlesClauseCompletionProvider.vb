' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(HandlesClauseCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ImplementsClauseCompletionProvider))>
    <[Shared]>
    Partial Friend Class HandlesClauseCompletionProvider
        Inherits AbstractSymbolCompletionProvider(Of VisualBasicSyntaxContext)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Async Function GetSymbolsAsync(
                completionContext As CompletionContext,
                syntaxContext As VisualBasicSyntaxContext,
                position As Integer,
                options As CompletionOptions,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndSelectionInfo))

            Dim symbols = Await GetSymbolsAsync(syntaxContext, position, cancellationToken).ConfigureAwait(False)
            Return symbols.SelectAsArray(Function(s) New SymbolAndSelectionInfo(s, Preselect:=False))
        End Function

        Private Overloads Shared Function GetSymbolsAsync(context As VisualBasicSyntaxContext, position As Integer, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))
            If context.SyntaxTree.IsInNonUserCode(position, cancellationToken) OrElse
                context.SyntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            If context.TargetToken.Kind = SyntaxKind.None Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            ' Handles or a comma
            If context.TargetToken.IsChildToken(Of HandlesClauseSyntax)(Function(hc) hc.HandlesKeyword) OrElse
                context.TargetToken.IsChildSeparatorToken(Function(hc As HandlesClauseSyntax) hc.Events) Then
                Return Task.FromResult(GetTopLevelIdentifiers(context, cancellationToken))
            End If

            ' Handles x. or , x.
            If context.TargetToken.IsChildToken(Of HandlesClauseItemSyntax)(Function(hc) hc.DotToken) Then
                Return Task.FromResult(LookUpEvents(context, context.TargetToken, cancellationToken))
            End If

            Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerChars

        Private Shared Function GetTopLevelIdentifiers(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As ImmutableArray(Of ISymbol)

            Dim containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken)
            Dim containingType = TryCast(containingSymbol, ITypeSymbol)
            If containingType Is Nothing Then
                ' We got the containing method as our enclosing type.
                containingType = containingSymbol.ContainingType
            End If

            If containingType Is Nothing Then
                ' We've somehow failed to find a containing type.
                Return ImmutableArray(Of ISymbol).Empty
            End If

            ' Instance or shared variables declared WithEvents
            Dim symbols = context.SemanticModel.LookupSymbols(context.Position, DirectCast(containingType, INamespaceOrTypeSymbol), includeReducedExtensionMethods:=True)
            Return symbols.WhereAsArray(Function(s) IsWithEvents(s))
        End Function

        Private Shared Function LookUpEvents(
            context As VisualBasicSyntaxContext,
            token As SyntaxToken,
            cancellationToken As CancellationToken
        ) As ImmutableArray(Of ISymbol)

            ' We came up on a dot, so the previous token will tell us in which object we should find events.
            Dim containingSymbol = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken)
            Dim containingType = TryCast(containingSymbol, ITypeSymbol)
            If containingType Is Nothing Then
                ' We got the containing method as our enclosing type.
                containingType = containingSymbol.ContainingType
            End If

            If containingType Is Nothing Then
                ' We've somehow failed to find a containing type.
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Dim result = ImmutableArray(Of IEventSymbol).Empty

            Dim previousToken = token.GetPreviousToken()
            Select Case previousToken.Kind
                Case SyntaxKind.MeKeyword, SyntaxKind.MyClassKeyword
                    result = context.SemanticModel.LookupSymbols(context.Position, containingType).
                        OfType(Of IEventSymbol)().
                        ToImmutableArray()
                Case SyntaxKind.MyBaseKeyword
                    result = context.SemanticModel.LookupSymbols(context.Position, containingType.BaseType).
                        OfType(Of IEventSymbol)().
                        ToImmutableArray()
                Case SyntaxKind.IdentifierToken
                    ' We must be looking at a WithEvents property.
                    Dim symbolInfo = context.SemanticModel.GetSymbolInfo(previousToken, cancellationToken)
                    If symbolInfo.Symbol IsNot Nothing Then
                        Dim type = TryCast(symbolInfo.Symbol, IPropertySymbol)?.Type
                        If type IsNot Nothing Then
                            result = context.SemanticModel.LookupSymbols(token.SpanStart, type).
                                OfType(Of IEventSymbol)().
                                ToImmutableArray()
                        End If
                    End If
            End Select

            Return ImmutableArray(Of ISymbol).CastUp(result)
        End Function

        Private Shared Function IsWithEvents(s As ISymbol) As Boolean
            Dim [property] = TryCast(s, IPropertySymbol)
            If [property] IsNot Nothing Then
                Return [property].IsWithEvents
            End If

            Return False
        End Function

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(
                symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)

            Return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context)
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch)
        End Function

        Protected Overrides Function CreateItem(completionContext As CompletionContext, displayText As String, displayTextSuffix As String, insertionText As String, symbols As ImmutableArray(Of SymbolAndSelectionInfo), context As VisualBasicSyntaxContext, supportedPlatformData As SupportedPlatformData) As CompletionItem
            Return CreateItemDefault(displayText, displayTextSuffix, insertionText, symbols, context, supportedPlatformData)
        End Function
    End Class
End Namespace
