' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class EnumCompletionProvider
        Inherits AbstractSymbolCompletionProvider

        Protected Overrides Function GetPreselectedSymbolsWorker(context As AbstractSyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol))
            If context.SyntaxTree.IsInNonUserCode(context.Position, cancellationToken) Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            Dim typeInferenceService = context.GetLanguageService(Of ITypeInferenceService)()
            Dim enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)

            If enumType.TypeKind <> TypeKind.Enum Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            Dim hideAdvancedMembers = options.GetOption(CodeAnalysis.Recommendations.RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language)

            ' We'll want to build a list of the actual enum members and all accessible instances of that enum, too
            Return Task.FromResult(enumType.GetMembers().Where(Function(m As ISymbol) As Boolean
                                                                   Return m.Kind = SymbolKind.Field AndAlso
                                                                      DirectCast(m, IFieldSymbol).IsConst AndAlso
                                                                      m.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation)
                                                               End Function))

        End Function

        Protected Overrides Function GetSymbolsWorker(context As AbstractSyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol))
            If context.SyntaxTree.IsInNonUserCode(context.Position, cancellationToken) OrElse
                context.SyntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            Dim typeInferenceService = context.GetLanguageService(Of ITypeInferenceService)()
            Dim span = New TextSpan(position, 0)
            Dim enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)

            If enumType.TypeKind <> TypeKind.Enum Then
                Return SpecializedTasks.EmptyEnumerable(Of ISymbol)()
            End If

            Dim hideAdvancedMembers = options.GetOption(CodeAnalysis.Recommendations.RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language)

            Dim otherSymbols = context.SemanticModel.LookupSymbols(position).Where(Function(s) s.MatchesKind(SymbolKind.Field, SymbolKind.Local, SymbolKind.Parameter, SymbolKind.Property) AndAlso
                                                                               s.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation))

            Dim otherInstances = otherSymbols.Where(Function(s) enumType Is GetTypeFromSymbol(s))

            Return Task.FromResult(otherInstances.Concat(enumType))
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return text(characterPosition) = " "c OrElse
                text(characterPosition) = "("c OrElse
                (text.Length > 2 AndAlso text(characterPosition) = "="c AndAlso text(characterPosition - 1) = ":"c) OrElse
                SyntaxFacts.IsIdentifierStartCharacter(text(characterPosition)) AndAlso
                options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic)
        End Function

        Private Function GetTypeFromSymbol(symbol As ISymbol) As ITypeSymbol
            Dim symbolType = symbol.TypeSwitch(Function(f As IFieldSymbol) f.Type,
                    Function(l As ILocalSymbol) l.Type,
                    Function(p As IParameterSymbol) p.Type,
                    Function(pr As IPropertySymbol) pr.Type)
            Return symbolType
        End Function

        ' PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        Private _cachedDisplayAndInsertionTextContainingType As INamedTypeSymbol
        Private _cachedDisplayAndInsertionTextContext As AbstractSyntaxContext
        Private _cachedDisplayAndInsertionTextContainingTypeText As String

        Protected Overrides Function GetDisplayAndInsertionText(symbol As ISymbol, context As AbstractSyntaxContext) As ValueTuple(Of String, String)
            If symbol.ContainingType IsNot Nothing AndAlso symbol.ContainingType.TypeKind = TypeKind.Enum Then
                If _cachedDisplayAndInsertionTextContainingType IsNot symbol.ContainingType OrElse _cachedDisplayAndInsertionTextContext IsNot context Then
                    Dim displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithLocalOptions(SymbolDisplayLocalOptions.None)
                    Dim displayService = context.GetLanguageService(Of ISymbolDisplayService)()
                    _cachedDisplayAndInsertionTextContainingTypeText = displayService.ToMinimalDisplayString(context.SemanticModel, context.Position, symbol.ContainingType, displayFormat)
                    _cachedDisplayAndInsertionTextContainingType = symbol.ContainingType
                    _cachedDisplayAndInsertionTextContext = context
                End If

                Dim text As String = _cachedDisplayAndInsertionTextContainingTypeText & "." & symbol.Name
                Return ValueTuple.Create(text, text)
            End If

            Dim result = CompletionUtilities.GetDisplayAndInsertionText(symbol,
                                                                  context.IsAttributeNameContext,
                                                                  context.IsRightOfNameSeparator,
                                                                  DirectCast(context, VisualBasicSyntaxContext).WithinAsyncMethod,
                                                                  context.GetLanguageService(Of ISyntaxFactsService)())
            Return result
        End Function

        Protected Overrides Function GetTextChangeSpan(text As SourceText, position As Integer) As TextSpan
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Protected Overrides Async Function CreateContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of AbstractSyntaxContext)
            Dim semanticModel = Await document.GetSemanticModelForSpanAsync(New TextSpan(position, 0), cancellationToken).ConfigureAwait(False)
            Return VisualBasicSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken)
        End Function

        Protected Overrides Function CreateItem(displayAndInsertionText As ValueTuple(Of String, String), position As Integer, symbols As List(Of ISymbol), context As AbstractSyntaxContext, textChangeSpan As TextSpan, preselect As Boolean, supportedPlatformData As SupportedPlatformData) As CompletionItem
            Return New SymbolCompletionItem(
                Me,
                displayAndInsertionText.Item1,
                displayAndInsertionText.Item2,
                GetFilterText(symbols(0), displayAndInsertionText.Item1, context),
                textChangeSpan,
                position,
                symbols,
                displayAndInsertionText.Item1,
                context,
                symbols(0).GetGlyph(),
                preselect:=preselect,
                supportedPlatforms:=supportedPlatformData,
                rules:=ItemRules.Instance)
        End Function

        Protected Overrides Function GetCompletionItemRules() As CompletionItemRules
            Return ItemRules.Instance
        End Function

    End Class
End Namespace
