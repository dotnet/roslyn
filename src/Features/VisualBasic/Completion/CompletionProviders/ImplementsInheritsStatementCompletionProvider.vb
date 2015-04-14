' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class ImplementsInheritsStatementCompletionProvider
        Inherits AbstractCompletionProvider

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean
            Return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar)
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean
            Return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar)
        End Function

        Protected Overrides Function IsExclusiveAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return SpecializedTasks.True
        End Function

        Private Class ImplementsInheritsCompletionItem
            Inherits CompletionItem

            Public ReadOnly InsertionText As String
            Public ReadOnly GenericInsertionText As String

            Public Sub New(provider As ImplementsInheritsStatementCompletionProvider,
                    displayText As String,
                    span As TextSpan,
                    description As Func(Of CancellationToken, Task(Of ImmutableArray(Of SymbolDisplayPart))),
                    glyph As Glyph?,
                    insertionText As String,
                    genericInsertionText As String)

                MyBase.New(provider, displayText, span, description, glyph)
                Me.InsertionText = insertionText
                Me.GenericInsertionText = genericInsertionText
            End Sub

        End Class

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim syntaxTree = Await document.GetVisualBasicSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position)
            If token.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            If syntaxTree.IsInNonUserCode(position, cancellationToken) OrElse
                syntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetVisualBasicSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(False)
            Dim typeBlock = token.GetAncestor(Of TypeBlockSyntax)()
            If typeBlock Is Nothing OrElse Not typeBlock.IsKind(SyntaxKind.ClassBlock, SyntaxKind.InterfaceBlock, SyntaxKind.StructureBlock) Then
                Return Nothing
            End If

            Dim symbolMatch As Func(Of ISymbol, ISymbol, Boolean) = Nothing

            ' Inherits statement
            If token.IsChildToken(Of InheritsStatementSyntax)(Function(i) i.InheritsKeyword) OrElse
               token.IsChildToken(Of QualifiedNameSyntax)(Function(qn) qn.DotToken) AndAlso token.Parent.GetAncestor(Of InheritsStatementSyntax)() IsNot Nothing Then

                ' Inherits shows only interfaces in interfaces
                If typeBlock.IsKind(SyntaxKind.InterfaceBlock) Then
                    symbolMatch = AddressOf InterfacePredicate
                Else
                    symbolMatch = AddressOf ClassPredicate
                End If
            End If

            ' Implements statement
            If token.IsChildToken(Of ImplementsStatementSyntax)(Function(i) i.ImplementsKeyword) OrElse
               token.IsChildSeparatorToken(Function(i As ImplementsStatementSyntax) i.Types) OrElse
               token.IsChildToken(Of QualifiedNameSyntax)(Function(qn) qn.DotToken) AndAlso token.GetAncestor(Of ImplementsStatementSyntax)() IsNot Nothing Then

                If Not typeBlock.IsKind(SyntaxKind.InterfaceBlock) Then
                    symbolMatch = AddressOf InterfacePredicate
                End If
            End If

            If symbolMatch Is Nothing Then
                Return Nothing
            End If

            Return Await GetCompletionsAsync(document.Project.Solution.Workspace, semanticModel, position, token, symbolMatch, cancellationToken).ConfigureAwait(False)
        End Function

        Public Overrides Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange
            Dim symbolItem = DirectCast(selectedItem, SymbolCompletionItem)

            Return CompletionUtilities.GetTextChange(symbolItem, ch, textTypedSoFar)
        End Function

        Private Async Function GetCompletionsAsync(workspace As Workspace,
                                                   semanticModel As SemanticModel,
                                                   position As Integer,
                                                   token As SyntaxToken,
                                                   predicate As Func(Of ISymbol, ISymbol, Boolean),
                                                   cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))

            Dim symbols As ImmutableArray(Of ISymbol) = Nothing

            If token.Kind = SyntaxKind.DotToken Then
                Dim left = DirectCast(token.Parent, QualifiedNameSyntax).Left
                Dim leftSymbol = semanticModel.GetSymbolInfo(left, cancellationToken)
                If leftSymbol.Symbol IsNot Nothing Then
                    symbols = semanticModel.LookupNamespacesAndTypes(position, TryCast(leftSymbol.Symbol, INamespaceOrTypeSymbol))
                End If
            Else
                symbols = SemanticModel.LookupNamespacesAndTypes(token.SpanStart)
            End If

            If symbols = Nothing OrElse symbols.Length = 0 Then
                Return Nothing
            End If

            Dim tokenPosition = token.SpanStart
            Dim text = Await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim span = CompletionUtilities.GetTextChangeSpan(text, position)
            Dim containingType = semanticModel.GetEnclosingNamedType(position, cancellationToken)

            Dim context = VisualBasicSyntaxContext.CreateContext(workspace, semanticModel, tokenPosition, cancellationToken)

            If containingType Is Nothing Then
                ' Try to figure out the containing type by finding the actual declaration.
                Dim typeBlock = token.GetAncestor(Of TypeBlockSyntax)()
                If typeBlock IsNot Nothing Then
                    Dim typeStatement = typeBlock.BlockStatement
                    If typeStatement IsNot Nothing Then
                        containingType = semanticModel.GetDeclaredSymbol(typeStatement, cancellationToken)
                    End If
                End If
            End If

            ' If something has gone horribly wrong and we're not inside a type at all, use the assembly symbol
            Dim within As ISymbol = containingType
            If within Is Nothing Then
                within = semanticModel.Compilation.Assembly
            End If

            Return symbols.Where(Function(s) predicate(s, within)) _
                          .Select(Function(s) CreateCompletionItem(tokenPosition, s, span, context))
        End Function

        Private Function CreateCompletionItem(
            position As Integer,
            symbol As ISymbol,
            span As TextSpan,
            context As VisualBasicSyntaxContext
        ) As CompletionItem

            Dim displayAndInsertionText = CompletionUtilities.GetDisplayAndInsertionText(symbol, isAttributeNameContext:=False, isAfterDot:=True, isWithinAsyncMethod:=False, syntaxFacts:=context.GetLanguageService(Of ISyntaxFactsService)())

            Return New SymbolCompletionItem(
                Me,
                displayAndInsertionText.Item1,
                displayAndInsertionText.Item2,
                span,
                position,
                {symbol}.ToList(),
                context)
        End Function

        Private Function InterfacePredicate(symbol As ISymbol, within As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Alias Then
                symbol = DirectCast(symbol, IAliasSymbol).Target
            End If

            Dim [namespace] = TryCast(symbol, INamespaceSymbol)
            If [namespace] IsNot Nothing Then
                Return [namespace].GetMembers().Any(Function(m) InterfacePredicate(m, within))
            End If

            Dim type = TryCast(symbol, INamedTypeSymbol)
            If type IsNot Nothing Then
                Return type.TypeKind = TypeKind.Interface OrElse
                    type.GetAccessibleMembersInThisAndBaseTypes(Of INamedTypeSymbol)(within) _
                        .Any(Function(m) ContainsOrIsInterface(m, within))
            End If

            Return False
        End Function

        Private Function ContainsOrIsInterface(typeOrNamespace As INamespaceOrTypeSymbol, within As ISymbol) As Boolean
            If typeOrNamespace.Kind = SymbolKind.Namespace Then
                Return InterfacePredicate(typeOrNamespace, within)
            End If

            Dim type = TryCast(typeOrNamespace, INamedTypeSymbol)
            If type IsNot Nothing AndAlso type.TypeKind = TypeKind.Interface Then
                Return True
            End If

            Dim members = type.GetMembers() _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(m) m.IsAccessibleWithin(within))
            Return members.Any(Function(m) ContainsOrIsInterface(m, within))
        End Function

        Private Function ClassPredicate(symbol As ISymbol, within As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Alias Then
                symbol = DirectCast(symbol, IAliasSymbol).Target
            End If

            Dim type = TryCast(symbol, ITypeSymbol)

            If type IsNot Nothing Then
                If type.TypeKind = TypeKind.Class AndAlso Not type.IsSealed AndAlso type IsNot within Then
                    Return True
                End If

                If type.TypeKind = TypeKind.Class OrElse type.TypeKind = TypeKind.Module OrElse type.TypeKind = TypeKind.Struct Then
                    Dim members = type.GetAccessibleMembersInThisAndBaseTypes(Of INamedTypeSymbol)(within)
                    Return members.Any(Function(m) ContainsOrIsClass(m, within))
                End If
            End If

            Dim namespaceSymbol = TryCast(symbol, INamespaceSymbol)
            If namespaceSymbol IsNot Nothing Then
                Return namespaceSymbol.GetMembers().Any(Function(m) ClassPredicate(m, within))
            End If

            Return False
        End Function

        Private Function ContainsOrIsClass(typeOrNamespace As INamespaceOrTypeSymbol, within As ISymbol) As Boolean
            If typeOrNamespace.Kind = SymbolKind.Namespace Then
                Return ClassPredicate(typeOrNamespace, within)
            End If

            Dim type = TryCast(typeOrNamespace, INamedTypeSymbol)
            If type IsNot Nothing AndAlso type.TypeKind = TypeKind.Class AndAlso Not type.IsSealed AndAlso type IsNot within Then
                Return True
            End If

            Dim members = type.GetMembers() _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(m) m.IsAccessibleWithin(within))
            Return members.Any(Function(m) ContainsOrIsClass(m, within))
        End Function
    End Class
End Namespace
