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
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(CompletionListTagCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(CrefCompletionProvider))>
    <[Shared]>
    Friend Class CompletionListTagCompletionProvider
        Inherits EnumCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides Function GetSymbolsAsync(
                completionContext As CompletionContext,
                syntaxContext As VisualBasicSyntaxContext,
                position As Integer,
                options As CompletionOptions,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndSelectionInfo))

            If syntaxContext.SyntaxTree.IsObjectCreationTypeContext(position, cancellationToken) OrElse
                syntaxContext.SyntaxTree.IsInNonUserCode(position, cancellationToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
            End If

            If syntaxContext.TargetToken.IsKind(SyntaxKind.DotToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
            End If

            Dim typeInferenceService = syntaxContext.GetLanguageService(Of ITypeInferenceService)()
            Dim inferredType = typeInferenceService.InferType(syntaxContext.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)
            If inferredType Is Nothing Then
                Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
            End If

            Dim within = syntaxContext.SemanticModel.GetEnclosingNamedType(position, cancellationToken)
            Dim completionListType = GetCompletionListType(inferredType, within, syntaxContext.SemanticModel.Compilation, cancellationToken)

            If completionListType Is Nothing Then
                Return SpecializedTasks.EmptyImmutableArray(Of SymbolAndSelectionInfo)()
            End If

            Dim builder = ArrayBuilder(Of SymbolAndSelectionInfo).GetInstance()
            For Each member In completionListType.GetAccessibleMembersInThisAndBaseTypes(Of ISymbol)(within)
                If member.MatchesKind(SymbolKind.Field, SymbolKind.Property) AndAlso
                    member.IsStatic AndAlso
                    member.IsAccessibleWithin(within) AndAlso
                    member.IsEditorBrowsable(options.HideAdvancedMembers, syntaxContext.SemanticModel.Compilation) Then
                    builder.Add(New SymbolAndSelectionInfo(member, Preselect:=True))
                End If
            Next

            Return Task.FromResult(builder.ToImmutableAndFree())
        End Function

        Private Shared Function GetCompletionListType(inferredType As ITypeSymbol, within As INamedTypeSymbol, compilation As Compilation, cancellationToken As CancellationToken) As ITypeSymbol
            Dim documentation = inferredType.GetDocumentationComment(compilation, expandIncludes:=True, expandInheritdoc:=True, cancellationToken:=cancellationToken)
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

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Dim displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithKindOptions(SymbolDisplayKindOptions.None)
            Dim text = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat)
            Return (text, "", text)
        End Function

        Protected Overrides Function CreateItem(
                completionContext As CompletionContext,
                displayText As String,
                displayTextSuffix As String,
                insertionText As String,
                symbols As ImmutableArray(Of SymbolAndSelectionInfo),
                context As VisualBasicSyntaxContext,
                supportedPlatformData As SupportedPlatformData) As CompletionItem

            ' Use symbol name (w/o containing type) as additional filter text, which would
            ' promote this item during matching when user types member name only, Like "Empty"
            ' instead of "ImmutableArray.Empty"
            Dim additionalFilterTexts = ImmutableArray.Create(symbols(0).Symbol.Name)
            Return SymbolCompletionItem.CreateWithSymbolId(
                displayText:=displayText,
                displayTextSuffix:=displayTextSuffix,
                insertionText:=insertionText,
                filterText:=displayText,
                symbols:=symbols.SelectAsArray(Function(t) t.Symbol),
                rules:=CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect),
                contextPosition:=context.Position,
                sortText:=displayText,
                supportedPlatforms:=supportedPlatformData,
                tags:=WellKnownTagArrays.TargetTypeMatch).WithAdditionalFilterTexts(additionalFilterTexts)
        End Function
    End Class
End Namespace
