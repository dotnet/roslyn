' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class PartialTypeCompletionProvider
        Inherits CompletionListProvider

        Private ReadOnly _partialNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle:=SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance Or
                                SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides Async Function ProduceCompletionListAsync(context As CompletionListContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim cancellationToken = context.CancellationToken

            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            If tree.IsInNonUserCode(position, cancellationToken) OrElse tree.IsInSkippedText(position, cancellationToken) Then
                Return
            End If

            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position)
            If token.IsChildToken(Of ClassStatementSyntax)(Function(stmt) stmt.DeclarationKeyword) OrElse
               token.IsChildToken(Of StructureStatementSyntax)(Function(stmt) stmt.DeclarationKeyword) OrElse
               token.IsChildToken(Of InterfaceStatementSyntax)(Function(stmt) stmt.DeclarationKeyword) OrElse
               token.IsChildToken(Of ModuleStatementSyntax)(Function(stmt) stmt.DeclarationKeyword) Then

                If token.GetAncestor(Of TypeStatementSyntax).Modifiers.Any(SyntaxKind.PartialKeyword) Then
                    Dim items = Await CreateItemsAsync(document, position, token, cancellationToken).ConfigureAwait(False)

                    If items?.Any() Then
                        context.AddItems(items)
                    End If
                End If
            End If
        End Function

        Private Async Function CreateItemsAsync(document As Document, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(False)

            ' Unless the enclosing symbol is already the global namespace, we want to get it's enclosing symbol
            ' in order to suggest partial types in our namespace.
            Dim enclosingSymbol = semanticModel.GetEnclosingSymbol(position, cancellationToken)
            Dim enclosingNamespace = TryCast(enclosingSymbol, INamespaceSymbol)
            If Not (enclosingNamespace IsNot Nothing AndAlso enclosingNamespace.IsGlobalNamespace) Then
                enclosingSymbol = enclosingSymbol.ContainingSymbol
            End If

            Dim displayService = document.GetLanguageService(Of ISymbolDisplayService)()

            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim textSpan = CompletionUtilities.GetTextChangeSpan(text, position)

            Dim compilation = semanticModel.Compilation
            Dim context = VisualBasicSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken)

            Return semanticModel.LookupNamespacesAndTypes(position) _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(s) NotNewDeclaredMember(s, token)) _
                .Where(Function(s) MatchesTypeKind(s, token) AndAlso InSameProject(s, compilation)) _
                .Select(Function(s) CreateCompletionItem(s, textSpan, displayService, token.SpanStart, context))
        End Function

        Private Function MatchesTypeKind(symbol As INamedTypeSymbol, token As SyntaxToken) As Boolean
            Select Case token.Kind
                Case SyntaxKind.ClassKeyword
                    Return symbol.TypeKind = TypeKind.Class

                Case SyntaxKind.StructureKeyword
                    Return symbol.TypeKind = TypeKind.Struct

                Case SyntaxKind.ModuleKeyword
                    Return symbol.TypeKind = TypeKind.Module

                Case SyntaxKind.InterfaceKeyword
                    Return symbol.TypeKind = TypeKind.Interface

                Case Else
                    Return False

            End Select

        End Function

        Private Function InSameProject(symbol As INamedTypeSymbol, compilation As Compilation) As Boolean
            Return symbol.DeclaringSyntaxReferences.Any(Function(r) compilation.SyntaxTrees.Contains(r.SyntaxTree))
        End Function

        Private Function CreateCompletionItem(symbol As INamedTypeSymbol,
                                              textSpan As TextSpan,
                                              displayService As ISymbolDisplayService,
                                              position As Integer,
                                              context As VisualBasicSyntaxContext) As CompletionItem
            Dim displayText As String = Nothing
            Dim insertionText As String = Nothing

            If symbol.MatchesKind(SymbolKind.NamedType) AndAlso symbol.GetArity() > 0 Then
                displayText = symbol.ToMinimalDisplayString(context.SemanticModel, position, format:=_partialNameFormat)
                insertionText = displayText
            Else
                Dim displayAndInsertionText = CompletionUtilities.GetDisplayAndInsertionText(symbol, isAttributeNameContext:=False, isAfterDot:=False, isWithinAsyncMethod:=False, syntaxFacts:=context.GetLanguageService(Of ISyntaxFactsService))
                displayText = displayAndInsertionText.Item1
                insertionText = displayAndInsertionText.Item2
            End If

            Return New SymbolCompletionItem(
                Me,
                displayText,
                insertionText,
                textSpan,
                position,
                {DirectCast(symbol, ISymbol)}.ToList(),
                context,
                rules:=ItemRules.Instance)
        End Function

        Private Function NotNewDeclaredMember(s As INamedTypeSymbol, token As SyntaxToken) As Boolean
            Return Not s.DeclaringSyntaxReferences.Select(Function(r) r.GetSyntax()).All(Function(a) a.Span.IntersectsWith(token.Span))
        End Function

    End Class
End Namespace
