' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class CompletionListTagCompletionProvider
        Inherits EnumCompletionProvider

        Protected Overrides Function GetPreselectedSymbolsWorker(context As SyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))
            If context.SyntaxTree.IsObjectCreationTypeContext(position, cancellationToken) OrElse
                context.SyntaxTree.IsInNonUserCode(position, cancellationToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim typeInferenceService = context.GetLanguageService(Of ITypeInferenceService)()
            Dim inferredType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)
            If inferredType Is Nothing Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim within = context.SemanticModel.GetEnclosingNamedType(position, cancellationToken)
            Dim completionListType = GetCompletionListType(inferredType, within, context.SemanticModel.Compilation)

            If completionListType Is Nothing Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim hideAdvancedMembers = options.GetOption(CodeAnalysis.Recommendations.RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language)

            Return Task.FromResult(completionListType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(within) _
                                                .WhereAsArray(Function(m) m.MatchesKind(SymbolKind.Field, SymbolKind.Property) AndAlso
                                                                    m.IsStatic AndAlso
                                                                    m.IsAccessibleWithin(within) AndAlso
                                                                    m.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation)))
        End Function

        Protected Overrides Function GetSymbolsWorker(context As SyntaxContext, position As Integer, options As OptionSet, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))
            Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
        End Function

        Private Function GetCompletionListType(inferredType As ITypeSymbol, within As INamedTypeSymbol, compilation As Compilation) As ITypeSymbol
            Dim documentation = inferredType.GetDocumentationComment()
            If documentation.CompletionListCref IsNot Nothing Then
                Dim crefType = DocumentationCommentId.GetSymbolsForDeclarationId(documentation.CompletionListCref, compilation) _
                                    .OfType(Of INamedTypeSymbol) _
                                    .FirstOrDefault()

                If crefType IsNot Nothing AndAlso crefType.IsAccessibleWithin(within) Then
                    Return crefType
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetDisplayAndInsertionText(symbol As ISymbol, context As SyntaxContext) As ValueTuple(Of String, String)
            Dim displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithKindOptions(SymbolDisplayKindOptions.None)
            Dim text = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat)
            Return ValueTuple.Create(text, text)
        End Function

        Protected Overrides Function CreateItem(displayText As String, insertionText As String, symbols As List(Of ISymbol), context As SyntaxContext, preselect As Boolean, supportedPlatformData As SupportedPlatformData) As CompletionItem
            Return SymbolCompletionItem.Create(
                displayText:=displayText,
                insertionText:=insertionText,
                filterText:=GetFilterText(symbols(0), displayText, context),
                symbols:=symbols,
                contextPosition:=context.Position,
                sortText:=displayText,
                glyph:=Glyph.EnumMember,
                matchPriority:=MatchPriority.Preselect,
                supportedPlatforms:=supportedPlatformData)
        End Function
    End Class
End Namespace