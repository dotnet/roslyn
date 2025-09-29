' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(EnumCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ObjectCreationCompletionProvider))>
    <[Shared]>
    Partial Friend Class EnumCompletionProvider
        Inherits AbstractSymbolCompletionProvider(Of VisualBasicSyntaxContext)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Private Shared ReadOnly s_enumMemberCompletionItemRules As CompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect)

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function GetSymbolsAsync(
                completionContext As CompletionContext,
                syntaxContext As VisualBasicSyntaxContext,
                position As Integer,
                options As CompletionOptions,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndSelectionInfo))

            Dim builder = ArrayBuilder(Of SymbolAndSelectionInfo).GetInstance()
            Try

                If syntaxContext.SyntaxTree.IsInNonUserCode(syntaxContext.Position, cancellationToken) Then
                    Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
                End If

                ' This providers provides fully qualified names, eg "DayOfWeek.Monday"
                ' Don't run after dot because SymbolCompletionProvider will provide
                ' members in situations like Dim x = DayOfWeek.$$
                If syntaxContext.TargetToken.IsKind(SyntaxKind.DotToken) Then
                    Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
                End If

                Dim typeInferenceService = syntaxContext.GetLanguageService(Of ITypeInferenceService)()
                Dim enumType = typeInferenceService.InferType(syntaxContext.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)

                If enumType.TypeKind <> TypeKind.Enum Then
                    Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
                End If

                builder.Add(New SymbolAndSelectionInfo(enumType, Preselect:=False))

                For Each member In enumType.GetMembers()
                    If member.Kind = SymbolKind.Field AndAlso DirectCast(member, IFieldSymbol).IsConst AndAlso member.IsEditorBrowsable(options.MemberDisplayOptions.HideAdvancedMembers, syntaxContext.SemanticModel.Compilation) Then
                        builder.Add(New SymbolAndSelectionInfo(member, Preselect:=True))
                    End If
                Next

                Return Task.FromResult(builder.ToImmutable())

            Finally
                builder.Free()
            End Try
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return text(characterPosition) = " "c OrElse
                text(characterPosition) = "("c OrElse
                (characterPosition > 1 AndAlso text(characterPosition) = "="c AndAlso text(characterPosition - 1) = ":"c) OrElse
                SyntaxFacts.IsIdentifierStartCharacter(text(characterPosition)) AndAlso
                options.TriggerOnTypingLetters
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet.Create(" "c, "("c, "="c)

        ' PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        Private ReadOnly _gate As New Object()
        Private _cachedDisplayAndInsertionTextContainingType As INamedTypeSymbol
        Private _cachedDisplayAndInsertionTextContext As VisualBasicSyntaxContext
        Private _cachedDisplayAndInsertionTextContainingTypeText As String

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            If symbol.Kind <> SymbolKind.Field Then
                Return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context)
            End If

            ' Completion service allows concurrent calls
            SyncLock _gate
                If Not Equals(_cachedDisplayAndInsertionTextContainingType, symbol.ContainingType) OrElse _cachedDisplayAndInsertionTextContext IsNot context Then
                    Dim displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithLocalOptions(SymbolDisplayLocalOptions.None)
                    Dim displayService = context.GetLanguageService(Of ISymbolDisplayService)()
                    _cachedDisplayAndInsertionTextContainingTypeText = symbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat)
                    _cachedDisplayAndInsertionTextContainingType = symbol.ContainingType
                    _cachedDisplayAndInsertionTextContext = context
                End If

                Dim text = _cachedDisplayAndInsertionTextContainingTypeText & "." & symbol.Name
                Return (text, "", text)
            End SyncLock
        End Function

        Protected Overrides Function CreateItem(
                completionContext As CompletionContext,
                displayText As String,
                displayTextSuffix As String,
                insertionText As String,
                symbols As ImmutableArray(Of SymbolAndSelectionInfo),
                context As VisualBasicSyntaxContext,
                supportedPlatformData As SupportedPlatformData) As CompletionItem

            Dim preselect = symbols.Any(Function(t) t.Preselect)
            Dim rules = If(preselect, s_enumMemberCompletionItemRules, CompletionItemRules.Default)

            Dim item = SymbolCompletionItem.CreateWithSymbolId(
                displayText:=displayText,
                displayTextSuffix:=displayTextSuffix,
                insertionText:=insertionText,
                filterText:=displayText,
                symbols:=symbols.SelectAsArray(Function(t) t.Symbol),
                contextPosition:=context.Position,
                sortText:=insertionText,
                supportedPlatforms:=supportedPlatformData,
                rules:=rules)

            ' Use member name (w/o enum type name) as additional filter text, which would
            ' promote this item during matching when user types member name only, Like "Red"
            ' instead of "Colors.Empty"
            If symbols(0).Symbol.Kind = SymbolKind.Field Then
                item = item.AddTag(WellKnownTags.TargetTypeMatch).WithAdditionalFilterTexts(ImmutableArray.Create(symbols(0).Symbol.Name))
            End If

            Return item
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch)
        End Function
    End Class
End Namespace
