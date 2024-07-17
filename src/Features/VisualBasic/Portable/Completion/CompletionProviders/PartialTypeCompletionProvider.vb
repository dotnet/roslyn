' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(PartialTypeCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(HandlesClauseCompletionProvider))>
    <[Shared]>
    Partial Friend Class PartialTypeCompletionProvider
        Inherits AbstractPartialTypeCompletionProvider(Of VisualBasicSyntaxContext)

        Private Const InsertionTextOnOpenParen As String = NameOf(InsertionTextOnOpenParen)

        Private Shared ReadOnly _insertionTextFormatWithGenerics As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                genericsOptions:=
                    SymbolDisplayGenericsOptions.IncludeTypeParameters Or
                    SymbolDisplayGenericsOptions.IncludeVariance Or
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints)

        Private Shared ReadOnly _displayTextFormat As SymbolDisplayFormat =
            _insertionTextFormatWithGenerics.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerChars

        Protected Overrides Function GetPartialTypeSyntaxNode(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim statement As TypeStatementSyntax = Nothing
            Return If(tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, statement), statement, Nothing)
        End Function

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As INamedTypeSymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            Dim displayText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format:=_displayTextFormat)
            Dim insertionText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, format:=_insertionTextFormatWithGenerics)
            Return (displayText, "", insertionText)
        End Function

        Protected Overrides Function GetProperties(symbol As INamedTypeSymbol, context As VisualBasicSyntaxContext) As ImmutableArray(Of KeyValuePair(Of String, String))
            Return ImmutableArray.Create(
                New KeyValuePair(Of String, String)(InsertionTextOnOpenParen, symbol.Name.EscapeIdentifier()))
        End Function

        Public Overrides Async Function GetTextChangeAsync(document As Document, selectedItem As CompletionItem, ch As Char?, cancellationToken As CancellationToken) As Task(Of TextChange?)
            If ch = "("c Then
                Dim insertionText As String = Nothing
                If selectedItem.TryGetProperty(InsertionTextOnOpenParen, insertionText) Then
                    Return New TextChange(selectedItem.Span, insertionText)
                End If
            End If

            Return Await MyBase.GetTextChangeAsync(document, selectedItem, ch, cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
