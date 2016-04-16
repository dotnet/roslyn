' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InlineTemporary), [Shared]>
    Partial Friend Class InlineTemporaryCodeRefactoringProvider
        Inherits CodeRefactoringProvider

        Public Overloads Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
            Dim document = context.Document
            Dim textSpan = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim workspace = document.Project.Solution.Workspace
            If workspace.Kind = WorkspaceKind.MiscellaneousFiles Then
                Return
            End If

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = DirectCast(root, SyntaxNode).FindToken(textSpan.Start)

            If Not token.Span.Contains(textSpan) Then
                Return
            End If

            Dim node = token.Parent

            If Not node.IsKind(SyntaxKind.ModifiedIdentifier) OrElse
               Not node.IsParentKind(SyntaxKind.VariableDeclarator) OrElse
               Not node.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement) Then

                Return
            End If

            Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
            Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim localDeclarationStatement = DirectCast(variableDeclarator.Parent, LocalDeclarationStatementSyntax)

            If modifiedIdentifier.Identifier <> token OrElse
               Not variableDeclarator.HasInitializer() Then

                Return
            End If

            If localDeclarationStatement.ParentingNodeContainsDiagnostics() Then
                Return
            End If

            Dim references = Await GetReferencesAsync(document, modifiedIdentifier, cancellationToken).ConfigureAwait(False)
            If Not references.Any() Then
                Return
            End If

            context.RegisterRefactoring(
                New MyCodeAction(VBFeaturesResources.InlineTemporaryVariable, Function(c) InlineTemporaryAsync(document, modifiedIdentifier, c)))
        End Function

        Private Async Function GetReferencesAsync(
            document As Document,
            modifiedIdentifier As ModifiedIdentifierSyntax,
            cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ReferenceLocation))

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim local = TryCast(semanticModel.GetDeclaredSymbol(modifiedIdentifier, cancellationToken), ILocalSymbol)

            If local IsNot Nothing Then
                Dim solution = document.Project.Solution
                Dim findReferencesResult = Await SymbolFinder.FindReferencesAsync(local, solution, cancellationToken).ConfigureAwait(False)

                Dim locations = findReferencesResult.Single(Function(r) r.Definition Is local).Locations
                If Not locations.Any(Function(loc) semanticModel.SyntaxTree.OverlapsHiddenPosition(loc.Location.SourceSpan, cancellationToken)) Then
                    Return locations
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of ReferenceLocation)()
        End Function

        Private Shared Function HasConflict(
            identifier As IdentifierNameSyntax,
            definition As ModifiedIdentifierSyntax,
            expressionToInline As ExpressionSyntax,
            semanticModel As SemanticModel
        ) As Boolean

            If identifier.SpanStart < definition.SpanStart Then
                Return True
            End If

            Dim identifierNode = identifier _
                .Ancestors() _
                .TakeWhile(Function(n)
                               Return n.Kind = SyntaxKind.ParenthesizedExpression OrElse
                                      TypeOf n Is CastExpressionSyntax OrElse
                                      TypeOf n Is PredefinedCastExpressionSyntax
                           End Function) _
                .LastOrDefault()

            If identifierNode Is Nothing Then
                identifierNode = identifier
            End If

            If TypeOf identifierNode.Parent Is AssignmentStatementSyntax Then
                Dim assignment = CType(identifierNode.Parent, AssignmentStatementSyntax)
                If assignment.Left Is identifierNode Then
                    Return True
                End If
            End If

            If TypeOf identifierNode.Parent Is ArgumentSyntax Then
                If TypeOf expressionToInline Is LiteralExpressionSyntax OrElse
                   TypeOf expressionToInline Is CastExpressionSyntax OrElse
                   TypeOf expressionToInline Is PredefinedCastExpressionSyntax Then

                    Dim argument = DirectCast(identifierNode.Parent, ArgumentSyntax)
                    Dim parameter = argument.DetermineParameter(semanticModel)
                    If parameter IsNot Nothing Then
                        Return parameter.RefKind <> RefKind.None
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared ReadOnly s_definitionAnnotation As New SyntaxAnnotation
        Private Shared ReadOnly s_referenceAnnotation As New SyntaxAnnotation
        Private Shared ReadOnly s_initializerAnnotation As New SyntaxAnnotation
        Private Shared ReadOnly s_expressionToInlineAnnotation As New SyntaxAnnotation

        Private Async Function InlineTemporaryAsync(document As Document, modifiedIdentifier As ModifiedIdentifierSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            ' First, annotate the modified identifier so that we can get back to it later.
            Dim updatedDocument = Await document.ReplaceNodeAsync(modifiedIdentifier, modifiedIdentifier.WithAdditionalAnnotations(s_definitionAnnotation), cancellationToken).ConfigureAwait(False)
            Dim semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)

            ' Create the expression that we're actually going to inline
            Dim expressionToInline = Await CreateExpressionToInlineAsync(updatedDocument, cancellationToken).ConfigureAwait(False)

            ' Collect the identifier names for each reference.
            Dim local = semanticModel.GetDeclaredSymbol(modifiedIdentifier, cancellationToken)
            Dim symbolRefs = Await SymbolFinder.FindReferencesAsync(local, updatedDocument.Project.Solution, cancellationToken).ConfigureAwait(False)
            Dim references = symbolRefs.Single(Function(r) r.Definition Is local).Locations
            Dim syntaxRoot = Await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' Collect the target statement for each reference.

            Dim nonConflictingIdentifierNodes = references _
                .Select(Function(loc) DirectCast(syntaxRoot.FindToken(loc.Location.SourceSpan.Start).Parent, IdentifierNameSyntax)) _
                .Where(Function(ident) Not HasConflict(ident, modifiedIdentifier, expressionToInline, semanticModel))

            ' Add referenceAnnotations to identifier nodes being replaced.
            updatedDocument = Await updatedDocument.ReplaceNodesAsync(
                nonConflictingIdentifierNodes,
                Function(o, n) n.WithAdditionalAnnotations(s_referenceAnnotation),
                cancellationToken).ConfigureAwait(False)

            semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)

            ' Get the annotated reference nodes.
            nonConflictingIdentifierNodes = Await FindReferenceAnnotatedNodesAsync(updatedDocument, cancellationToken).ConfigureAwait(False)

            Dim topMostStatements = nonConflictingIdentifierNodes _
                .Select(Function(ident) GetTopMostStatementForExpression(ident))

            ' Next, get the top-most statement of the local declaration
            Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim localDeclaration = DirectCast(variableDeclarator.Parent, LocalDeclarationStatementSyntax)
            Dim originalInitializerSymbolInfo = semanticModel.GetSymbolInfo(variableDeclarator.GetInitializer())

            Dim topMostStatementOfLocalDeclaration = If(localDeclaration.HasAncestor(Of ExpressionSyntax),
                                                        localDeclaration.Ancestors().OfType(Of ExpressionSyntax).Last().FirstAncestorOrSelf(Of StatementSyntax)(),
                                                        localDeclaration)

            topMostStatements = topMostStatements.Concat(topMostStatementOfLocalDeclaration)

            ' Next get the statements before and after the top-most statement of the local declaration
            Dim previousStatement = topMostStatementOfLocalDeclaration.GetPreviousStatement()
            If previousStatement IsNot Nothing Then
                topMostStatements = topMostStatements.Concat(previousStatement)
            End If

            ' Now, add the statement *after* each top-level statement.
            Dim nextStatements = topMostStatements _
                .Select(Function(stmt) stmt.GetNextStatement()) _
                .WhereNotNull()

            topMostStatements = topMostStatements _
                .Concat(nextStatements) _
                .Distinct()

            ' Make each target statement semantically explicit.
            updatedDocument = Await updatedDocument.ReplaceNodesAsync(
                topMostStatements,
                Function(o, n)
                    Return Simplifier.Expand(DirectCast(n, StatementSyntax), semanticModel, document.Project.Solution.Workspace, cancellationToken:=cancellationToken)
                End Function,
                cancellationToken).ConfigureAwait(False)

            semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModelBeforeInline = semanticModel

            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)
            Dim scope = GetScope(modifiedIdentifier)
            Dim newScope = ReferenceRewriter.Visit(semanticModel, scope, modifiedIdentifier, expressionToInline, cancellationToken)

            updatedDocument = Await updatedDocument.ReplaceNodeAsync(scope, newScope.WithAdditionalAnnotations(Formatter.Annotation), cancellationToken).ConfigureAwait(False)
            semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)
            newScope = GetScope(modifiedIdentifier)
            Dim conflicts = newScope.GetAnnotatedNodesAndTokens(ConflictAnnotation.Kind)
            Dim declaratorConflicts = modifiedIdentifier.GetAnnotatedNodesAndTokens(ConflictAnnotation.Kind)

            ' Note that we only remove the local declaration if there weren't any conflicts,
            ' unless those conflicts are inside the local declaration.
            If conflicts.Count() = declaratorConflicts.Count() Then
                ' Certain semantic conflicts can be detected only after the reference rewriter has inlined the expression
                Dim newDocument = Await DetectSemanticConflicts(updatedDocument,
                                                                semanticModel,
                                                                semanticModelBeforeInline,
                                                                originalInitializerSymbolInfo,
                                                                cancellationToken).ConfigureAwait(False)
                If updatedDocument Is newDocument Then
                    ' No semantic conflicts, we can remove the definition.
                    updatedDocument = Await updatedDocument.ReplaceNodeAsync(newScope, RemoveDefinition(modifiedIdentifier, newScope), cancellationToken).ConfigureAwait(False)
                Else
                    ' There were some semantic conflicts, don't remove the definition.
                    updatedDocument = newDocument
                End If
            End If

            Return updatedDocument
        End Function

        Private Shared Async Function FindDefinitionAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ModifiedIdentifierSyntax)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim result = root _
                .GetAnnotatedNodesAndTokens(s_definitionAnnotation) _
                .Single() _
                .AsNode()

            Return DirectCast(result, ModifiedIdentifierSyntax)
        End Function

        Private Shared Async Function FindReferenceAnnotatedNodesAsync(document As Document, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of IdentifierNameSyntax))
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Return FindReferenceAnnotatedNodes(root)
        End Function

        Private Shared Iterator Function FindReferenceAnnotatedNodes(root As SyntaxNode) As IEnumerable(Of IdentifierNameSyntax)
            Dim annotatedNodesAndTokens = root.GetAnnotatedNodesAndTokens(s_referenceAnnotation)

            For Each nodeOrToken In annotatedNodesAndTokens
                If nodeOrToken.IsKind(SyntaxKind.IdentifierName) Then
                    Yield DirectCast(nodeOrToken.AsNode(), IdentifierNameSyntax)
                End If
            Next
        End Function

        Private Shared Function GetScope(modifiedIdentifier As ModifiedIdentifierSyntax) As SyntaxNode
            Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim localDeclaration = DirectCast(variableDeclarator.Parent, LocalDeclarationStatementSyntax)

            Return localDeclaration.Parent
        End Function

        Private Function GetUpdatedDeclaration(modifiedIdentifier As ModifiedIdentifierSyntax) As LocalDeclarationStatementSyntax
            Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim localDeclaration = DirectCast(variableDeclarator.Parent, LocalDeclarationStatementSyntax)

            If localDeclaration.Declarators.Count > 1 And variableDeclarator.Names.Count = 1 Then
                Return localDeclaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepEndOfLine)
            End If

            If variableDeclarator.Names.Count > 1 Then
                Return localDeclaration.RemoveNode(modifiedIdentifier, SyntaxRemoveOptions.KeepEndOfLine)
            End If

            Contract.Fail("Failed to update local declaration")
            Return localDeclaration
        End Function

        Private Function RemoveDefinition(modifiedIdentifier As ModifiedIdentifierSyntax, newBlock As SyntaxNode) As SyntaxNode
            Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim localDeclaration = DirectCast(variableDeclarator.Parent, LocalDeclarationStatementSyntax)

            If variableDeclarator.Names.Count > 1 OrElse
               localDeclaration.Declarators.Count > 1 Then

                ' In this case, we need to remove the definition from either the declarators or the names of
                ' the local declaration.
                Dim newDeclaration = GetUpdatedDeclaration(modifiedIdentifier) _
                    .WithAdditionalAnnotations(Formatter.Annotation)

                Dim newStatements = newBlock.GetExecutableBlockStatements().Replace(localDeclaration, newDeclaration)
                Return newBlock.ReplaceStatements(newStatements)
            Else
                ' In this case, we're removing the local declaration. Care must be taken to move any
                ' non-whitespace trivia to the next statement.

                Dim blockStatements = newBlock.GetExecutableBlockStatements()
                Dim declarationIndex = blockStatements.IndexOf(localDeclaration)

                Dim leadingTrivia = localDeclaration _
                    .GetLeadingTrivia() _
                    .Reverse() _
                    .SkipWhile(Function(t) t.IsKind(SyntaxKind.WhitespaceTrivia)) _
                    .Reverse()

                Dim trailingTrivia = localDeclaration _
                    .GetTrailingTrivia() _
                    .SkipWhile(Function(t) t.IsKind(SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia, SyntaxKind.ColonTrivia))

                Dim newLeadingTrivia = leadingTrivia.Concat(trailingTrivia)

                ' Ensure that we leave a line break if our local declaration ended with a comment.
                If newLeadingTrivia.Any() AndAlso newLeadingTrivia.Last().IsKind(SyntaxKind.CommentTrivia) Then
                    newLeadingTrivia = newLeadingTrivia.Concat(SyntaxFactory.CarriageReturnLineFeed)
                End If

                Dim nextToken = localDeclaration.GetLastToken().GetNextToken()
                Dim newNextToken = nextToken _
                    .WithPrependedLeadingTrivia(newLeadingTrivia.ToSyntaxTriviaList()) _
                    .WithAdditionalAnnotations(Formatter.Annotation)

                Dim previousToken = localDeclaration.GetFirstToken().GetPreviousToken()

                ' If the previous token has trailing colon trivia, replace it with a new line.
                Dim previousTokenTrailingTrivia = previousToken.TrailingTrivia.ToList()
                If previousTokenTrailingTrivia.Count > 0 AndAlso previousTokenTrailingTrivia.Last().IsKind(SyntaxKind.ColonTrivia) Then
                    previousTokenTrailingTrivia(previousTokenTrailingTrivia.Count - 1) = SyntaxFactory.CarriageReturnLineFeed
                End If

                Dim newPreviousToken = previousToken _
                    .WithTrailingTrivia(previousTokenTrailingTrivia) _
                    .WithAdditionalAnnotations(Formatter.Annotation)

                newBlock = newBlock.ReplaceTokens({previousToken, nextToken},
                    Function(oldToken, newToken)
                        If oldToken = nextToken Then
                            Return newNextToken
                        ElseIf oldToken = previousToken Then
                            Return newPreviousToken
                        Else
                            Return newToken
                        End If
                    End Function)

                Dim newBlockStatements = newBlock.GetExecutableBlockStatements()
                Dim newStatements = newBlockStatements.RemoveAt(declarationIndex)

                Return newBlock.ReplaceStatements(newStatements)
            End If
        End Function

        Private Shared Function AddExplicitArgumentListIfNeeded(expression As ExpressionSyntax, semanticModel As SemanticModel) As ExpressionSyntax
            If expression.IsKind(SyntaxKind.IdentifierName) OrElse
               expression.IsKind(SyntaxKind.GenericName) OrElse
               expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then

                Dim symbol = semanticModel.GetSymbolInfo(expression).Symbol
                If symbol IsNot Nothing AndAlso
                  (symbol.Kind = SymbolKind.Method OrElse symbol.Kind = SymbolKind.Property) Then

                    Dim trailingTrivia = expression.GetTrailingTrivia()

                    Return SyntaxFactory _
                        .InvocationExpression(
                            expression:=expression.WithTrailingTrivia(CType(Nothing, SyntaxTriviaList)),
                            argumentList:=SyntaxFactory.ArgumentList().WithTrailingTrivia(trailingTrivia)) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)
                End If
            End If

            Return expression
        End Function

        Private Shared Async Function CreateExpressionToInlineAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ExpressionSyntax)
            ' TODO: We should be using a speculative semantic model in the method rather than forking new semantic model every time.

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim modifiedIdentifier = Await FindDefinitionAsync(document, cancellationToken).ConfigureAwait(False)
            Dim initializer = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).GetInitializer()
            Dim newInitializer = AddExplicitArgumentListIfNeeded(initializer, semanticModel) _
                                 .WithAdditionalAnnotations(s_initializerAnnotation)

            Dim updatedDocument = Await document.ReplaceNodeAsync(initializer, newInitializer, cancellationToken).ConfigureAwait(False)

            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)
            initializer = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).GetInitializer()

            Dim explicitInitializer = Await Simplifier.ExpandAsync(initializer, updatedDocument, cancellationToken:=cancellationToken).ConfigureAwait(False)

            Dim lastToken = explicitInitializer.GetLastToken()
            explicitInitializer = explicitInitializer.ReplaceToken(lastToken, lastToken.WithTrailingTrivia(SyntaxTriviaList.Empty))

            updatedDocument = Await updatedDocument.ReplaceNodeAsync(initializer, explicitInitializer, cancellationToken).ConfigureAwait(False)
            semanticModel = Await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            modifiedIdentifier = Await FindDefinitionAsync(updatedDocument, cancellationToken).ConfigureAwait(False)
            explicitInitializer = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).GetInitializer()

            Dim local = DirectCast(semanticModel.GetDeclaredSymbol(modifiedIdentifier, cancellationToken), ILocalSymbol)
            Dim wasCastAdded As Boolean = False
            explicitInitializer = explicitInitializer.CastIfPossible(local.Type,
                                                                     modifiedIdentifier.SpanStart,
                                                                     semanticModel,
                                                                     wasCastAdded)

            Return explicitInitializer.WithAdditionalAnnotations(s_expressionToInlineAnnotation)
        End Function

        Private Shared Function GetTopMostStatementForExpression(expression As ExpressionSyntax) As StatementSyntax
            Return expression.AncestorsAndSelf().OfType(Of ExpressionSyntax).Last().FirstAncestorOrSelf(Of StatementSyntax)()
        End Function

        Private Shared Async Function DetectSemanticConflicts(
            inlinedDocument As Document,
            newSemanticModelForInlinedDocument As SemanticModel,
            semanticModelBeforeInline As SemanticModel,
            originalInitializerSymbolInfo As SymbolInfo,
            cancellationToken As CancellationToken
        ) As Task(Of Document)

            ' In this method we detect if inlining the expression introduced the following semantic change:
            '  The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.

            ' If any semantic changes were introduced by inlining, we update the document with conflict annotations.
            ' Otherwise we return the given inlined document without any changes.

            Dim syntaxRootBeforeInline = Await semanticModelBeforeInline.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            ' Get all the identifier nodes which were replaced with inlined expression.
            Dim originalIdentifierNodes = FindReferenceAnnotatedNodes(syntaxRootBeforeInline).ToArray()

            If originalIdentifierNodes.IsEmpty Then
                ' No conflicts
                Return inlinedDocument
            End If

            ' Get all the inlined expression nodes.
            Dim syntaxRootAfterInline = Await inlinedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim inlinedExprNodes = syntaxRootAfterInline.GetAnnotatedNodesAndTokens(s_expressionToInlineAnnotation).ToArray()
            Debug.Assert(originalIdentifierNodes.Length = inlinedExprNodes.Length)

            Dim replacementNodesWithChangedSemantics As Dictionary(Of SyntaxNode, SyntaxNode) = Nothing

            For i = 0 To originalIdentifierNodes.Length - 1
                Dim originalNode = originalIdentifierNodes(i)
                Dim inlinedNode = DirectCast(inlinedExprNodes(i).AsNode(), ExpressionSyntax)

                ' inlinedNode is the expanded form of the actual initializer expression in the original document.
                ' We have annotated the inner initializer with a special syntax annotation "_initializerAnnotation".
                ' Get this annotated node and compute the symbol info for this node in the inlined document.
                Dim innerInitializerInInlineNode = DirectCast(inlinedNode.GetAnnotatedNodesAndTokens(s_initializerAnnotation).Single().AsNode, ExpressionSyntax)
                Dim newInitializerSymbolInfo = newSemanticModelForInlinedDocument.GetSymbolInfo(innerInitializerInInlineNode, cancellationToken)

                ' Verification: The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.
                If Not SpeculationAnalyzer.SymbolInfosAreCompatible(originalInitializerSymbolInfo, newInitializerSymbolInfo, performEquivalenceCheck:=True) Then
                    If replacementNodesWithChangedSemantics Is Nothing Then
                        replacementNodesWithChangedSemantics = New Dictionary(Of SyntaxNode, SyntaxNode)
                    End If

                    replacementNodesWithChangedSemantics.Add(inlinedNode, originalNode)
                End If
            Next

            If replacementNodesWithChangedSemantics Is Nothing Then
                ' No conflicts.
                Return inlinedDocument
            End If

            ' Replace the conflicting inlined nodes with the original nodes annotated with conflict annotation.
            Dim conflictAnnotationAdder = Function(oldNode As SyntaxNode, newNode As SyntaxNode) As SyntaxNode
                                              Return newNode _
                                                  .WithAdditionalAnnotations(ConflictAnnotation.Create(VBFeaturesResources.ConflictsDetected))
                                          End Function
            Return Await inlinedDocument.ReplaceNodesAsync(replacementNodesWithChangedSemantics.Keys, conflictAnnotationAdder, cancellationToken).ConfigureAwait(False)
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
