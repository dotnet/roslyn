' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename

    Friend Class VisualBasicRenameRewriterLanguageService
        Implements IRenameRewriterLanguageService

        Private ReadOnly languageServiceProvider As ILanguageServiceProvider

        Public Sub New(provider As ILanguageServiceProvider)
            languageServiceProvider = provider
        End Sub

#Region "Annotate"

        Public Function AnnotateAndRename(parameters As RenameRewriterParameters) As SyntaxNode Implements IRenameRewriterLanguageService.AnnotateAndRename
            Dim renameRewriter = New RenameRewriter(parameters)
            Return renameRewriter.Visit(parameters.SyntaxRoot)
        End Function

        Private Class RenameRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly documentId As DocumentId
            Private ReadOnly renameRenamableSymbolDeclaration As RenameAnnotation
            Private ReadOnly solution As Solution
            Private ReadOnly replacementText As String
            Private ReadOnly originalText As String
            Private ReadOnly possibleNameConflicts As ICollection(Of String)
            Private ReadOnly renameLocations As Dictionary(Of TextSpan, RenameLocation)
            Private ReadOnly conflictLocations As IEnumerable(Of TextSpan)
            Private ReadOnly semanticModel As SemanticModel
            Private ReadOnly cancellationToken As CancellationToken
            Private ReadOnly renamedSymbol As ISymbol
            Private ReadOnly aliasSymbol As IAliasSymbol
            Private ReadOnly renamableDeclarationLocation As Location
            Private ReadOnly renameSpansTracker As RenamedSpansTracker
            Private ReadOnly isVerbatim As Boolean
            Private ReadOnly replacementTextValid As Boolean
            Private ReadOnly isRenamingInStrings As Boolean
            Private ReadOnly isRenamingInComments As Boolean
            Private ReadOnly stringAndCommentTextSpans As ISet(Of TextSpan)
            Private ReadOnly simplificationService As ISimplificationService
            Private ReadOnly annotatedIdentifierTokens As New HashSet(Of SyntaxToken)
            Private ReadOnly invocationExpressionsNeedingConflictChecks As New HashSet(Of InvocationExpressionSyntax)
            Private ReadOnly syntaxFactsService As ISyntaxFactsService
            Private ReadOnly renameAnnotations As AnnotationTable(Of RenameAnnotation)

            Private ReadOnly Property AnnotateForComplexification As Boolean
                Get
                    Return Me.skipRenameForComplexification > 0 AndAlso Not Me.isProcessingComplexifiedSpans
                End Get
            End Property

            Private skipRenameForComplexification As Integer = 0
            Private isProcessingComplexifiedSpans As Boolean
            Private modifiedSubSpans As List(Of ValueTuple(Of TextSpan, TextSpan)) = Nothing
            Private speculativeModel As SemanticModel
            Private isProcessingStructuredTrivia As Integer
            Private complexifiedSpans As HashSet(Of TextSpan) = New HashSet(Of TextSpan)

            Private Sub AddModifiedSpan(oldSpan As TextSpan, newSpan As TextSpan)
                newSpan = New TextSpan(oldSpan.Start, newSpan.Length)
                If Not Me.isProcessingComplexifiedSpans Then
                    renameSpansTracker.AddModifiedSpan(documentId, oldSpan, newSpan)
                Else
                    Me.modifiedSubSpans.Add(ValueTuple.Create(oldSpan, newSpan))
                End If
            End Sub

            Public Sub New(parameters As RenameRewriterParameters)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me.documentId = parameters.Document.Id
                Me.renameRenamableSymbolDeclaration = parameters.RenamedSymbolDeclarationAnnotation
                Me.solution = parameters.OriginalSolution
                Me.replacementText = parameters.ReplacementText
                Me.originalText = parameters.OriginalText
                Me.possibleNameConflicts = parameters.PossibleNameConflicts
                Me.renameLocations = parameters.RenameLocations
                Me.conflictLocations = parameters.ConflictLocationSpans
                Me.cancellationToken = parameters.CancellationToken
                Me.semanticModel = DirectCast(parameters.SemanticModel, SemanticModel)
                Me.renamedSymbol = parameters.RenameSymbol
                Me.replacementTextValid = parameters.ReplacementTextValid
                Me.renameSpansTracker = parameters.RenameSpansTracker
                Me.isRenamingInStrings = parameters.OptionSet.GetOption(RenameOptions.RenameInStrings)
                Me.isRenamingInComments = parameters.OptionSet.GetOption(RenameOptions.RenameInComments)
                Me.stringAndCommentTextSpans = parameters.StringAndCommentTextSpans
                Me.aliasSymbol = TryCast(Me.renamedSymbol, IAliasSymbol)
                Me.renamableDeclarationLocation = Me.renamedSymbol.Locations.Where(Function(loc) loc.IsInSource AndAlso loc.SourceTree Is semanticModel.SyntaxTree).FirstOrDefault()
                Me.simplificationService = LanguageService.GetService(Of ISimplificationService)(parameters.Document)
                Me.syntaxFactsService = LanguageService.GetService(Of ISyntaxFactsService)(parameters.Document)
                Me.isVerbatim = Me.syntaxFactsService.IsVerbatimIdentifier(replacementText)
                Me.renameAnnotations = parameters.RenameAnnotations
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                Dim isInConflictLambdaBody = False
                Dim lambdas = node.GetAncestorsOrThis(Of MultiLineLambdaExpressionSyntax)()
                If lambdas.Count() <> 0 Then
                    For Each lambda In lambdas
                        If Me.conflictLocations.Any(Function(cf)
                                                        Return cf.Contains(lambda.Span)
                                                    End Function) Then
                            isInConflictLambdaBody = True
                            Exit For
                        End If
                    Next
                End If

                Dim shouldComplexifyNode =
                    Not isInConflictLambdaBody AndAlso
                    Me.skipRenameForComplexification = 0 AndAlso
                    Not Me.isProcessingComplexifiedSpans AndAlso
                    Me.conflictLocations.Contains(node.Span)

                Dim result As SyntaxNode
                If shouldComplexifyNode Then
                    Me.skipRenameForComplexification += 1
                    result = MyBase.Visit(node)
                    Me.skipRenameForComplexification -= 1
                    result = Complexify(node, result)
                Else
                    result = MyBase.Visit(node)
                End If

                Return result
            End Function

            Private Function Complexify(originalNode As SyntaxNode, newNode As SyntaxNode) As SyntaxNode
                If Me.complexifiedSpans.Contains(originalNode.Span) Then
                    Return newNode
                Else
                    Me.complexifiedSpans.Add(originalNode.Span)
                End If

                Me.isProcessingComplexifiedSpans = True
                Me.modifiedSubSpans = New List(Of ValueTuple(Of TextSpan, TextSpan))()
                Dim annotation = New SyntaxAnnotation()

                newNode = newNode.WithAdditionalAnnotations(annotation)
                Dim speculativeTree = originalNode.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(originalNode, newNode)
                newNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                Me.speculativeModel = GetSemanticModelForNode(newNode, originalNode.Span.Start, Me.semanticModel)
                Debug.Assert(speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")

                Dim oldSpan = originalNode.Span

                Dim expandParameter = originalNode.GetAncestorsOrThis(Of LambdaExpressionSyntax).Count() = 0

                Dim expandedNewNode = DirectCast(simplificationService.Expand(newNode,
                                                                  speculativeModel,
                                                                  annotationForReplacedAliasIdentifier:=Nothing,
                                                                  expandInsideNode:=AddressOf IsExpandWithinMultiLineLambda,
                                                                  expandParameter:=expandParameter,
                                                                  cancellationToken:=cancellationToken), SyntaxNode)
                Dim annotationForSpeculativeNode = New SyntaxAnnotation()
                expandedNewNode = expandedNewNode.WithAdditionalAnnotations(annotationForSpeculativeNode)
                speculativeTree = originalNode.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(originalNode, expandedNewNode)
                Dim probableRenameNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                Dim speculativeNewNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotationForSpeculativeNode).First()

                Me.speculativeModel = GetSemanticModelForNode(speculativeNewNode, originalNode.Span.Start, Me.semanticModel)
                Debug.Assert(speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")
                Dim renamedNode = MyBase.Visit(probableRenameNode)

                If Not ReferenceEquals(renamedNode, probableRenameNode) Then
                    renamedNode = renamedNode.WithoutAnnotations(annotation)
                    probableRenameNode = expandedNewNode.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                    expandedNewNode = expandedNewNode.ReplaceNode(probableRenameNode, renamedNode)
                End If

                Dim newSpan = expandedNewNode.Span
                probableRenameNode = probableRenameNode.WithoutAnnotations(annotation)
                expandedNewNode = Me.renameAnnotations.WithAdditionalAnnotations(expandedNewNode, New RenameNodeSimplificationAnnotation() With {.OriginalTextSpan = oldSpan})

                Me.renameSpansTracker.AddComplexifiedSpan(Me.documentId, oldSpan, New TextSpan(oldSpan.Start, newSpan.Length), Me.modifiedSubSpans)
                Me.modifiedSubSpans = Nothing
                Me.isProcessingComplexifiedSpans = False
                Me.speculativeModel = Nothing
                Return expandedNewNode
            End Function

            Private Function IsExpandWithinMultiLineLambda(node As SyntaxNode) As Boolean
                If node Is Nothing Then
                    Return False
                End If

                If Me.conflictLocations.Contains(node.Span) Then
                    Return True
                End If

                If node.IsParentKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                node.IsParentKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then
                    Dim parent = DirectCast(node.Parent, MultiLineLambdaExpressionSyntax)
                    If ReferenceEquals(parent.Begin, node) Then
                        Return True
                    Else
                        Return False
                    End If
                End If

                Return True
            End Function

            Private Function IsPossibleNameConflict(possibleNameConflicts As ICollection(Of String), candidate As String) As Boolean
                For Each possibleNameConflict In possibleNameConflicts
                    If CaseInsensitiveComparison.Compare(possibleNameConflict, candidate) = 0 Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Private Function UpdateAliasAnnotation(newToken As SyntaxToken) As SyntaxToken
                If Me.aliasSymbol IsNot Nothing AndAlso Not Me.AnnotateForComplexification AndAlso newToken.HasAnnotations(AliasAnnotation.Kind) Then
                    newToken = CType(RenameUtilities.UpdateAliasAnnotation(newToken, Me.aliasSymbol, Me.replacementText), SyntaxToken)
                End If

                Return newToken
            End Function

            Private Function RenameAndAnnotate(token As SyntaxToken, newToken As SyntaxToken, isRenameLocation As Boolean, isOldText As Boolean) As SyntaxToken
                If Me.isProcessingComplexifiedSpans Then
                    If isRenameLocation Then
                        Dim annotation = Me.renameAnnotations.GetAnnotations(Of RenameActionAnnotation)(token).FirstOrDefault()
                        If annotation IsNot Nothing Then
                            newToken = RenameToken(token, newToken, annotation.Suffix, annotation.IsAccessorLocation)
                            AddModifiedSpan(annotation.OriginalSpan, New TextSpan(token.Span.Start, newToken.Span.Length))
                        Else
                            newToken = RenameToken(token, newToken, suffix:=Nothing, isAccessorLocation:=False)
                        End If
                    End If

                    Return newToken
                End If

                Dim symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, Me.semanticModel, Me.solution.Workspace, Me.cancellationToken)

                ' this is the compiler generated backing field of a non custom event. We need to store a "Event" suffix to properly rename it later on.
                Dim suffix As String = Nothing
                If symbols.Count() = 1 Then
                    Dim symbol = symbols.Single()

                    If symbol.IsConstructor() Then
                        symbol = symbol.ContainingSymbol
                    End If

                    If symbol.Kind = SymbolKind.Field AndAlso symbol.IsImplicitlyDeclared Then
                        Dim fieldSymbol = DirectCast(symbol, IFieldSymbol)

                        If fieldSymbol.Type.IsDelegateType AndAlso
                        fieldSymbol.Type.IsImplicitlyDeclared AndAlso
                        DirectCast(fieldSymbol.Type, INamedTypeSymbol).AssociatedSymbol IsNot Nothing Then

                            suffix = "Event"
                        End If

                    ElseIf symbol.IsConstructor AndAlso
                     symbol.ContainingType.IsImplicitlyDeclared AndAlso
                     symbol.ContainingType.IsDelegateType AndAlso
                     symbol.ContainingType.AssociatedSymbol IsNot Nothing Then

                        suffix = "EventHandler"
                    ElseIf TypeOf symbol Is INamedTypeSymbol Then
                        Dim namedTypeSymbol = DirectCast(symbol, INamedTypeSymbol)
                        If namedTypeSymbol.IsImplicitlyDeclared AndAlso
                        namedTypeSymbol.IsDelegateType() AndAlso
                        namedTypeSymbol.AssociatedSymbol IsNot Nothing Then
                            suffix = "EventHandler"
                        End If
                    End If

                    If Not isRenameLocation AndAlso TypeOf (symbol) Is INamespaceSymbol AndAlso token.GetPreviousToken().VisualBasicKind = SyntaxKind.NamespaceKeyword Then
                        Return newToken
                    End If
                End If

                If isRenameLocation AndAlso Not Me.AnnotateForComplexification Then
                    Dim oldSpan = token.Span
                    newToken = RenameToken(token, newToken, suffix:=suffix, isAccessorLocation:=isRenameLocation AndAlso Me.renameLocations(token.Span).IsRenamableAccessor)
                    AddModifiedSpan(oldSpan, newToken.Span)
                End If

                Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                    ConflictResolver.CreateDeclarationLocationAnnotationsAsync(solution, symbols, cancellationToken).WaitAndGetResult_Hack(cancellationToken)

                Dim isNamespaceDeclarationReference = False
                If isRenameLocation AndAlso token.GetPreviousToken().VisualBasicKind = SyntaxKind.NamespaceKeyword Then
                    isNamespaceDeclarationReference = True
                End If

                Dim renameAnnotation = New RenameActionAnnotation(
                                    token.Span,
                                    isRenameLocation,
                                    If(isRenameLocation, Me.renameLocations(token.Span).IsRenamableAccessor, False),
                                    suffix,
                                    isOldText,
                                    renameDeclarationLocations,
                                    isNamespaceDeclarationReference,
                                                    isInvocationExpression:=False)

                annotatedIdentifierTokens.Add(token)
                newToken = Me.renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = token.Span})
                If Me.renameRenamableSymbolDeclaration IsNot Nothing AndAlso renamableDeclarationLocation = token.GetLocation() Then
                    newToken = Me.renameAnnotations.WithAdditionalAnnotations(newToken, Me.renameRenamableSymbolDeclaration)
                End If

                Return newToken
            End Function

            Private Function IsInRenameLocation(token As SyntaxToken) As Boolean
                If Not Me.isProcessingComplexifiedSpans Then
                    Return Me.renameLocations.ContainsKey(token.Span)
                Else
                    If token.HasAnnotations(AliasAnnotation.Kind) Then
                        Return False
                    End If

                    If Me.renameAnnotations.HasAnnotations(Of RenameActionAnnotation)(token) Then
                        Return Me.renameAnnotations.GetAnnotations(Of RenameActionAnnotation)(token).First().IsRenameLocation
                    End If

                    If TypeOf token.Parent Is SimpleNameSyntax AndAlso token.VisualBasicKind <> SyntaxKind.GlobalKeyword AndAlso token.Parent.Parent.MatchesKind(SyntaxKind.QualifiedName, SyntaxKind.QualifiedCrefOperatorReference) Then
                        Dim symbol = Me.speculativeModel.GetSymbolInfo(token.Parent, Me.cancellationToken).Symbol
                        If symbol IsNot Nothing AndAlso Me.renamedSymbol.Kind <> SymbolKind.Local AndAlso Me.renamedSymbol.Kind <> SymbolKind.RangeVariable AndAlso
                            (symbol Is Me.renamedSymbol OrElse SymbolKey.GetComparer(ignoreCase:=True, ignoreAssemblyKeys:=False).Equals(symbol.GetSymbolKey(), Me.renamedSymbol.GetSymbolKey())) Then
                            Return True
                        End If
                    End If

                    Return False
                End If
            End Function

            Public Overrides Function VisitToken(oldToken As SyntaxToken) As SyntaxToken
                If oldToken = Nothing Then
                    Return oldToken
                End If

                Dim newToken = oldToken
                Dim shouldCheckTrivia = Me.stringAndCommentTextSpans.Contains(oldToken.Span)
                If shouldCheckTrivia Then
                    Me.isProcessingStructuredTrivia += 1
                    newToken = MyBase.VisitToken(newToken)
                    Me.isProcessingStructuredTrivia -= 1
                Else
                    newToken = MyBase.VisitToken(newToken)
                End If

                newToken = UpdateAliasAnnotation(newToken)

                ' Rename matches in strings and comments
                newToken = RenameWithinToken(oldToken, newToken)

                ' We don't want to annotate XmlName with RenameActionAnnotation
                If newToken.VisualBasicKind = SyntaxKind.XmlNameToken Then
                    Return newToken
                End If

                Dim isRenameLocation = IsInRenameLocation(oldToken)
                Dim isOldText = CaseInsensitiveComparison.Compare(oldToken.ValueText, originalText) = 0
                Dim tokenNeedsConflictCheck = isRenameLocation OrElse
                    isOldText OrElse
                    CaseInsensitiveComparison.Compare(oldToken.ValueText, replacementText) = 0 OrElse
                    IsPossibleNameConflict(possibleNameConflicts, oldToken.ValueText)

                If tokenNeedsConflictCheck Then
                    newToken = RenameAndAnnotate(oldToken, newToken, isRenameLocation, isOldText)

                    If Not Me.isProcessingComplexifiedSpans Then
                        invocationExpressionsNeedingConflictChecks.AddRange(oldToken.GetAncestors(Of InvocationExpressionSyntax)())
                    End If
                End If

                Return newToken
            End Function

            Private Function GetAnnotationForInvocationExpression(invocationExpression As InvocationExpressionSyntax) As RenameActionAnnotation
                Dim identifierToken As SyntaxToken = Nothing
                Dim expressionOfInvocation = invocationExpression.Expression
                While expressionOfInvocation IsNot Nothing
                    Select Case expressionOfInvocation.VisualBasicKind
                        Case SyntaxKind.IdentifierName, SyntaxKind.GenericName
                            identifierToken = DirectCast(expressionOfInvocation, SimpleNameSyntax).Identifier
                            Exit While
                        Case SyntaxKind.SimpleMemberAccessExpression
                            identifierToken = DirectCast(expressionOfInvocation, MemberAccessExpressionSyntax).Name.Identifier
                            Exit While
                        Case SyntaxKind.QualifiedName
                            identifierToken = DirectCast(expressionOfInvocation, QualifiedNameSyntax).Right.Identifier
                            Exit While
                        Case SyntaxKind.ParenthesizedExpression
                            expressionOfInvocation = DirectCast(expressionOfInvocation, ParenthesizedExpressionSyntax).Expression
                        Case SyntaxKind.MeExpression
                            Exit While
                        Case SyntaxKind.SingleLineSubLambdaExpression,
                                SyntaxKind.SingleLineFunctionLambdaExpression,
                                SyntaxKind.MultiLineSubLambdaExpression,
                                SyntaxKind.MultiLineFunctionLambdaExpression,
                                SyntaxKind.InvocationExpression
                            Return Nothing
                        Case Else
                            ExceptionUtilities.UnexpectedValue(expressionOfInvocation.VisualBasicKind)
                    End Select
                End While

                If identifierToken <> Nothing AndAlso Not Me.annotatedIdentifierTokens.Contains(identifierToken) Then
                    Dim symbolInfo = Me.semanticModel.GetSymbolInfo(invocationExpression, Me.cancellationToken)
                    Dim symbols As IEnumerable(Of ISymbol) = Nothing
                    If symbolInfo.Symbol Is Nothing Then
                        Return Nothing
                    Else
                        symbols = SpecializedCollections.SingletonEnumerable(symbolInfo.Symbol)
                    End If

                    Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                        ConflictResolver.CreateDeclarationLocationAnnotationsAsync(solution, symbols, cancellationToken).WaitAndGetResult_Hack(cancellationToken)

                    Dim renameAnnotation = New RenameActionAnnotation(
                                            identifierToken.Span,
                                            isRenameLocation:=False,
                                            isAccessorLocation:=False,
                                            suffix:=Nothing,
                                            renameDeclarationLocations:=renameDeclarationLocations,
                                            isOriginalTextLocation:=False,
                                            isNamespaceDeclarationReference:=False,
                                            isInvocationExpression:=True)

                    Return renameAnnotation
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                Dim result = MyBase.VisitInvocationExpression(node)
                If invocationExpressionsNeedingConflictChecks.Contains(node) Then
                    Dim renameAnnotation = GetAnnotationForInvocationExpression(node)
                    If renameAnnotation IsNot Nothing Then
                        result = Me.renameAnnotations.WithAdditionalAnnotations(result, renameAnnotation)
                    End If
                End If

                Return result
            End Function

            Private Function RenameToken(oldToken As SyntaxToken, newToken As SyntaxToken, suffix As String, isAccessorLocation As Boolean) As SyntaxToken
                Dim parent = oldToken.Parent
                Dim currentNewIdentifier = Me.replacementText
                Dim oldIdentifier = newToken.ValueText
                Dim isAttributeName = SyntaxFacts.IsAttributeName(parent)
                If isAttributeName Then
                    Debug.Assert(Me.renamedSymbol.IsAttribute() OrElse Me.aliasSymbol.Target.IsAttribute())
                    If oldIdentifier <> Me.renamedSymbol.Name Then
                        Dim withoutSuffix = String.Empty
                        If currentNewIdentifier.TryReduceAttributeSuffix(withoutSuffix) Then
                            currentNewIdentifier = withoutSuffix
                        End If
                    End If
                ElseIf isAccessorLocation Then
                    Dim prefix = oldIdentifier.Substring(0, oldIdentifier.IndexOf("_") + 1)
                    currentNewIdentifier = prefix + currentNewIdentifier
                ElseIf Not String.IsNullOrEmpty(suffix) Then
                    currentNewIdentifier = currentNewIdentifier + suffix
                End If

                ' determine the canonical identifier name (unescaped, no type char, ...)
                Dim valueText = currentNewIdentifier
                Dim name = SyntaxFactory.ParseName(currentNewIdentifier)
                If name.ContainsDiagnostics Then
                    name = SyntaxFactory.IdentifierName(currentNewIdentifier)
                End If

                If name.IsKind(SyntaxKind.GlobalName) Then
                    valueText = currentNewIdentifier
                Else
                    Debug.Assert(name.IsKind(SyntaxKind.IdentifierName))
                    valueText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                End If

                If Me.isVerbatim Then
                    newToken = newToken.CopyAnnotationsTo(SyntaxFactory.BracketedIdentifier(newToken.LeadingTrivia, valueText, newToken.TrailingTrivia))
                Else
                    newToken = newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(
                                                          newToken.LeadingTrivia,
                                                          If(oldToken.GetTypeCharacter() = TypeCharacter.None, currentNewIdentifier, currentNewIdentifier + oldToken.ToString().Last()),
                                                          False,
                                                          valueText,
                                                      oldToken.GetTypeCharacter(),
                                                          newToken.TrailingTrivia))

                    If Me.replacementTextValid AndAlso
                        oldToken.GetTypeCharacter() <> TypeCharacter.None AndAlso
                        (SyntaxFacts.GetKeywordKind(valueText) = SyntaxKind.REMKeyword OrElse Me.syntaxFactsService.IsVerbatimIdentifier(newToken)) Then

                        newToken = Me.renameAnnotations.WithAdditionalAnnotations(newToken, RenameInvalidIdentifierAnnotation.Instance)
                    End If
                End If

                If Me.replacementTextValid Then
                    If newToken.IsBracketed Then
                        ' a reference location should always be tried to be unescaped, whether it was escaped before rename 
                        ' or the replacement itself is escaped.
                        newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation)
                    Else
                        Dim semanticModel = GetSemanticModelForNode(parent, parent.Span.Start, If(Me.speculativeModel, Me.semanticModel))
                        newToken = Simplification.VisualBasicSimplificationService.TryEscapeIdentifierToken(newToken, semanticModel, oldToken)
                    End If
                End If

                Return newToken
            End Function

            Private Function RenameInStringLiteral(oldToken As SyntaxToken, newToken As SyntaxToken, createNewStringLiteral As Func(Of SyntaxTriviaList, String, String, SyntaxTriviaList, SyntaxToken)) As SyntaxToken
                Dim originalString = newToken.ToString()
                Dim replacedString As String = RenameLocationSet.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, originalText, replacementText)
                If replacedString <> originalString Then
                    Dim oldSpan = oldToken.Span
                    newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia)
                    AddModifiedSpan(oldSpan, newToken.Span)
                    Return newToken.CopyAnnotationsTo(Me.renameAnnotations.WithAdditionalAnnotations(newToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
                End If

                Return newToken
            End Function

            Private Function RenameInCommentTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim originalString = trivia.ToString()
                Dim replacedString As String = RenameLocationSet.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, originalText, replacementText)
                If replacedString <> originalString Then
                    Dim oldSpan = trivia.Span
                    Dim newTrivia = SyntaxFactory.CommentTrivia(replacedString)
                    AddModifiedSpan(oldSpan, newTrivia.Span)
                    Return trivia.CopyAnnotationsTo(Me.renameAnnotations.WithAdditionalAnnotations(newTrivia, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
                End If

                Return trivia
            End Function

            Private Function RenameInTrivia(token As SyntaxToken, leadingOrTrailingTriviaList As IEnumerable(Of SyntaxTrivia)) As SyntaxToken
                Return token.ReplaceTrivia(leadingOrTrailingTriviaList, Function(oldTrivia, newTrivia)
                                                                            If newTrivia.VisualBasicKind = SyntaxKind.CommentTrivia Then
                                                                                Return RenameInCommentTrivia(newTrivia)
                                                                            End If

                                                                            Return newTrivia
                                                                        End Function)
            End Function

            Private Function RenameWithinToken(oldToken As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
                If Me.isProcessingComplexifiedSpans OrElse
                (Me.isProcessingStructuredTrivia = 0 AndAlso Not Me.stringAndCommentTextSpans.Contains(oldToken.Span)) Then
                    Return newToken
                End If

                If Me.isRenamingInStrings AndAlso newToken.VisualBasicKind = SyntaxKind.StringLiteralToken Then
                    newToken = RenameInStringLiteral(oldToken, newToken, AddressOf SyntaxFactory.StringLiteralToken)
                End If

                If Me.isRenamingInComments Then
                    If newToken.VisualBasicKind = SyntaxKind.XmlTextLiteralToken Then
                        newToken = RenameInStringLiteral(oldToken, newToken, AddressOf SyntaxFactory.XmlTextLiteralToken)
                    ElseIf newToken.VisualBasicKind = SyntaxKind.XmlNameToken AndAlso CaseInsensitiveComparison.Compare(oldToken.ValueText, originalText) = 0 Then
                        Dim newIdentifierToken = SyntaxFactory.XmlNameToken(newToken.LeadingTrivia, replacementText, SyntaxFacts.GetKeywordKind(replacementText), newToken.TrailingTrivia)
                        newToken = newToken.CopyAnnotationsTo(Me.renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldToken.Span}))
                        AddModifiedSpan(oldToken.Span, newToken.Span)
                    End If

                    If newToken.HasLeadingTrivia Then
                        Dim updatedToken = RenameInTrivia(oldToken, oldToken.LeadingTrivia)
                        If updatedToken <> oldToken Then
                            newToken = newToken.WithLeadingTrivia(updatedToken.LeadingTrivia)
                        End If
                    End If

                    If newToken.HasTrailingTrivia Then
                        Dim updatedToken = RenameInTrivia(oldToken, oldToken.TrailingTrivia)
                        If updatedToken <> oldToken Then
                            newToken = newToken.WithTrailingTrivia(updatedToken.TrailingTrivia)
                        End If
                    End If
                End If

                Return newToken
            End Function

        End Class

#End Region

#Region "Declaration Conflicts"

        Public Function LocalVariableConflict(
            token As SyntaxToken
            ) As Boolean Implements IRenameRewriterLanguageService.LocalVariableConflict

            ' This scenario is not present in VB and only in C#
            Return False
        End Function

        Public Function ComputeDeclarationConflictsAsync(
            replacementText As String,
            renamedSymbol As ISymbol,
            renameSymbol As ISymbol,
            referencedSymbols As IEnumerable(Of ISymbol),
            baseSolution As Solution,
            newSolution As Solution,
            reverseMappedLocations As IDictionary(Of Location, Location),
            cancellationToken As CancellationToken
        ) As Task(Of IEnumerable(Of Location)) Implements IRenameRewriterLanguageService.ComputeDeclarationConflictsAsync
            Dim conflicts As New List(Of Location)

            If renamedSymbol.Kind = SymbolKind.Parameter OrElse
               renamedSymbol.Kind = SymbolKind.Local OrElse
               renamedSymbol.Kind = SymbolKind.RangeVariable Then

                Dim token = renamedSymbol.Locations.Single().FindToken(cancellationToken)

                ' Find the method block or field declaration that we're in. Note the LastOrDefault
                ' so we find the uppermost one, since VariableDeclarators live in methods too.
                Dim methodBase = token.Parent.AncestorsAndSelf.Where(Function(s) TypeOf s Is MethodBlockBaseSyntax OrElse TypeOf s Is VariableDeclaratorSyntax) _
                                                              .LastOrDefault()

                Dim visitor As New LocalConflictVisitor(token, newSolution, cancellationToken)
                visitor.Visit(methodBase)

                conflicts.AddRange(visitor.ConflictingTokens.Select(Function(t) t.GetLocation()) _
                               .Select(Function(loc) reverseMappedLocations(loc)))

                ' in VB parameters of properties are not allowed to be the same as the containing property
                If renamedSymbol.Kind = SymbolKind.Parameter AndAlso
                    renamedSymbol.ContainingSymbol.Kind = SymbolKind.Property AndAlso
                    CaseInsensitiveComparison.Compare(renamedSymbol.ContainingSymbol.Name, renamedSymbol.Name) = 0 Then

                    Dim propertySymbol = renamedSymbol.ContainingSymbol

                    While propertySymbol IsNot Nothing
                        conflicts.AddRange(renamedSymbol.ContainingSymbol.Locations _
                                       .Select(Function(loc) reverseMappedLocations(loc)))

                        propertySymbol = propertySymbol.OverriddenMember
                    End While
                End If

            ElseIf renamedSymbol.Kind = SymbolKind.Label Then
                Dim token = renamedSymbol.Locations.Single().FindToken(cancellationToken)
                Dim containingMethod = token.Parent.FirstAncestorOrSelf(Of SyntaxNode)(
                    Function(s) TypeOf s Is MethodBlockBaseSyntax OrElse
                                TypeOf s Is LambdaExpressionSyntax)

                Dim visitor As New LabelConflictVisitor(token)
                visitor.Visit(containingMethod)
                conflicts.AddRange(visitor.ConflictingTokens.Select(Function(t) t.GetLocation()) _
                    .Select(Function(loc) reverseMappedLocations(loc)))

            ElseIf renamedSymbol.Kind = SymbolKind.Method Then
                conflicts.AddRange(
                    DeclarationConflictHelpers.GetMembersWithConflictingSignatures(DirectCast(renamedSymbol, IMethodSymbol), trimOptionalParameters:=True) _
                        .Select(Function(loc) reverseMappedLocations(loc)))

            ElseIf renamedSymbol.Kind = SymbolKind.Property Then
                ConflictResolver.AddConflictingParametersOfProperties(referencedSymbols.Concat(renameSymbol).Where(Function(sym) sym.Kind = SymbolKind.Property),
                                                 renamedSymbol.Name,
                                                 conflicts)

            ElseIf renamedSymbol.Kind = SymbolKind.TypeParameter Then
                Dim Location = renamedSymbol.Locations.Single()
                Dim token = renamedSymbol.Locations.Single().FindToken(cancellationToken)
                Dim currentTypeParameter = token.Parent

                For Each typeParameter In DirectCast(currentTypeParameter.Parent, TypeParameterListSyntax).Parameters
                    If typeParameter IsNot currentTypeParameter AndAlso CaseInsensitiveComparison.Compare(token.ValueText, typeParameter.Identifier.ValueText) = 0 Then
                        conflicts.Add(reverseMappedLocations(typeParameter.Identifier.GetLocation()))
                    End If
                Next
            End If

            ' if the renamed symbol is a type member, it's name should not coflict with a type parameter
            If renamedSymbol.ContainingType IsNot Nothing AndAlso renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol) Then
                For Each typeParameter In renamedSymbol.ContainingType.TypeParameters
                    If CaseInsensitiveComparison.Compare(typeParameter.Name, renamedSymbol.Name) = 0 Then
                        Dim typeParameterToken = typeParameter.Locations.Single().FindToken(cancellationToken)
                        conflicts.Add(reverseMappedLocations(typeParameterToken.GetLocation()))
                    End If
                Next
            End If

            Return Task.FromResult(Of IEnumerable(Of Location))(conflicts)
        End Function

        Public Function ComputeImplicitReferenceConflicts(renameSymbol As ISymbol, renamedSymbol As ISymbol, implicitReferenceLocations As IEnumerable(Of ReferenceLocation), cancellationToken As CancellationToken) As IEnumerable(Of Location) Implements IRenameRewriterLanguageService.ComputeImplicitReferenceConflicts

            ' Handle renaming of symbols used for foreach
            Dim implicitReferencesMightConflict = renameSymbol.Kind = SymbolKind.Property AndAlso
                                                CaseInsensitiveComparison.Compare(renameSymbol.Name, "Current") = 0
            implicitReferencesMightConflict = implicitReferencesMightConflict OrElse
                                                (renameSymbol.Kind = SymbolKind.Method AndAlso
                                                    (CaseInsensitiveComparison.Compare(renameSymbol.Name, "MoveNext") = 0 OrElse
                                                    CaseInsensitiveComparison.Compare(renameSymbol.Name, "GetEnumerator") = 0))

            ' TODO: handle Dispose for using statement and Add methods for collection initializers.

            If implicitReferencesMightConflict Then
                If CaseInsensitiveComparison.Compare(renamedSymbol.Name, renameSymbol.Name) <> 0 Then
                    For Each implicitReferenceLocation In implicitReferenceLocations
                        Dim token = implicitReferenceLocation.Location.SourceTree.GetTouchingToken(implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, False)

                        If token.VisualBasicKind = SyntaxKind.ForKeyword AndAlso token.IsParentKind(SyntaxKind.ForEachStatement) Then
                            Return SpecializedCollections.SingletonEnumerable(Of Location)(DirectCast(token.Parent, ForEachStatementSyntax).Expression.GetLocation())
                        End If
                    Next
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of Location)()
        End Function

#End Region

        ''' <summary>
        ''' Gets the top most enclosing statement as target to call MakeExplicit on.
        ''' It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        ''' statement of this lambda.
        ''' </summary>
        ''' <param name="token">The token to get the complexification target for.</param>
        ''' <returns></returns>
        Public Function GetExpansionTargetForLocation(token As SyntaxToken) As SyntaxNode Implements IRenameRewriterLanguageService.GetExpansionTargetForLocation
            Return GetExpansionTarget(token)
        End Function

        Private Shared Function GetExpansionTarget(token As SyntaxToken) As SyntaxNode
            ' get the directly enclosing statement
            Dim enclosingStatement = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is ExecutableStatementSyntax)

            ' for nodes in a using, for or for each statement, we do not need the enclosing _executable_ statement, which is the whole block.
            ' it's enough to expand the using, for or foreach statement.
            Dim possibleSpecialStatement = token.FirstAncestorOrSelf(Function(n) n.VisualBasicKind = SyntaxKind.ForStatement OrElse
                                                                                 n.VisualBasicKind = SyntaxKind.ForEachStatement OrElse
                                                                                 n.VisualBasicKind = SyntaxKind.UsingStatement OrElse
                                                                                 n.VisualBasicKind = SyntaxKind.CatchPart)
            If possibleSpecialStatement IsNot Nothing Then
                If enclosingStatement Is possibleSpecialStatement.Parent Then
                    enclosingStatement = If(possibleSpecialStatement.VisualBasicKind = SyntaxKind.CatchPart,
                                                DirectCast(possibleSpecialStatement, CatchPartSyntax).Begin,
                                                possibleSpecialStatement)
                End If
            End If

            ' see if there's an enclosing lambda expression
            Dim possibleLambdaExpression As SyntaxNode = Nothing
            If enclosingStatement Is Nothing Then
                possibleLambdaExpression = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is LambdaExpressionSyntax)
            End If

            Dim enclosingCref = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is CrefReferenceSyntax)
            If enclosingCref IsNot Nothing Then
                Return enclosingCref
            End If

            ' there seems to be no statement above this one. Let's see if we can at least get an SimpleNameSyntax
            Return If(enclosingStatement, If(possibleLambdaExpression, token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is SimpleNameSyntax)))
        End Function

#Region "Helper Methods"
        Public Function IsIdentifierValid(replacementText As String, syntaxFactsService As ISyntaxFactsService) As Boolean Implements IRenameRewriterLanguageService.IsIdentifierValid
            replacementText = SyntaxFacts.MakeHalfWidthIdentifier(replacementText)
            Dim possibleIdentifier As String
            If syntaxFactsService.IsTypeCharacter(replacementText.Last()) Then
                ' We don't allow to use identifiers with type characters
                Return False
            Else
                If replacementText.StartsWith("[") AndAlso replacementText.EndsWith("]") Then
                    possibleIdentifier = replacementText
                Else
                    possibleIdentifier = "[" & replacementText & "]"
                End If
            End If

            ' Make sure we got an identifier. 
            If Not syntaxFactsService.IsValidIdentifier(possibleIdentifier) Then
                ' We still don't have an identifier, so let's fail
                Return False
            End If

            ' This is a valid Identifier
            Return True
        End Function

        Public Function ComputePossibleImplicitUsageConflicts(
            renamedSymbol As ISymbol,
            semanticModel As SemanticModel,
            originalDeclarationLocation As Location,
            newDeclarationLocationStartingPosition As Integer,
            cancellationToken As CancellationToken) As IEnumerable(Of Location) Implements IRenameRewriterLanguageService.ComputePossibleImplicitUsageConflicts

            ' TODO: support other implicitly used methods like dispose
            If CaseInsensitiveComparison.Compare(renamedSymbol.Name, "MoveNext") = 0 OrElse
                    CaseInsensitiveComparison.Compare(renamedSymbol.Name, "GetEnumerator") = 0 OrElse
                    CaseInsensitiveComparison.Compare(renamedSymbol.Name, "Current") = 0 Then


                If TypeOf renamedSymbol Is IMethodSymbol Then
                    If DirectCast(renamedSymbol, IMethodSymbol).IsOverloads AndAlso
                            (renamedSymbol.GetAllTypeArguments().Length <> 0 OrElse
                            DirectCast(renamedSymbol, IMethodSymbol).Parameters.Length <> 0) Then
                        Return SpecializedCollections.EmptyEnumerable(Of Location)()
                    End If
                End If

                If TypeOf renamedSymbol Is IPropertySymbol Then
                    If DirectCast(renamedSymbol, IPropertySymbol).IsOverloads Then
                        Return SpecializedCollections.EmptyEnumerable(Of Location)()
                    End If
                End If

                ' TODO: Partial methods currently only show the location where the rename happens As a conflict.
                '       Consider showing both locations as a conflict.

                Dim baseType = renamedSymbol.ContainingType.GetBaseTypes().FirstOrDefault()
                If baseType IsNot Nothing Then
                    Dim implicitSymbols = semanticModel.LookupSymbols(
                            newDeclarationLocationStartingPosition,
                            baseType,
                            renamedSymbol.Name) _
                                .Where(Function(sym) Not sym.Equals(renamedSymbol))

                    For Each symbol In implicitSymbols
                        If symbol.GetAllTypeArguments().Length <> 0 Then
                            Continue For
                        End If

                        If symbol.Kind = SymbolKind.Method Then
                            Dim method = DirectCast(symbol, IMethodSymbol)

                            If CaseInsensitiveComparison.Compare(symbol.Name, "MoveNext") = 0 Then
                                If Not method.ReturnsVoid AndAlso Not method.Parameters.Any() AndAlso method.ReturnType.SpecialType = SpecialType.System_Boolean Then
                                    Return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation)
                                End If
                            ElseIf CaseInsensitiveComparison.Compare(symbol.Name, "GetEnumerator") = 0 Then
                                ' we are a bit pessimistic here. 
                                ' To be sure we would need to check if the returned type Is having a MoveNext And Current as required by foreach
                                If Not method.ReturnsVoid AndAlso
                                        Not method.Parameters.Any() Then
                                    Return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation)
                                End If
                            End If

                        ElseIf CaseInsensitiveComparison.Compare(symbol.Name, "Current") = 0 Then
                            Dim [property] = DirectCast(symbol, IPropertySymbol)

                            If Not [property].Parameters.Any() AndAlso Not [property].IsWriteOnly Then
                                Return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation)
                            End If
                        End If
                    Next
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of Location)()
        End Function

        Public Sub TryAddPossibleNameConflicts(symbol As ISymbol, replacementText As String, possibleNameConflicts As ICollection(Of String)) Implements IRenameRewriterLanguageService.TryAddPossibleNameConflicts
            Dim halfWidthReplacementText = SyntaxFacts.MakeHalfWidthIdentifier(replacementText)

            Const AttributeSuffix As String = "Attribute"
            Const AttributeSuffixLength As Integer = 9
            Debug.Assert(AttributeSuffixLength = AttributeSuffix.Length, "Assert (AttributeSuffixLength = AttributeSuffix.Length) failed.")

            If replacementText.Length > AttributeSuffixLength AndAlso CaseInsensitiveComparison.Compare(halfWidthReplacementText.Substring(halfWidthReplacementText.Length - AttributeSuffixLength), AttributeSuffix) = 0 Then
                Dim conflict = replacementText.Substring(0, replacementText.Length - AttributeSuffixLength)
                If Not possibleNameConflicts.Contains(conflict) Then
                    possibleNameConflicts.Add(conflict)
                End If
            End If

            If symbol.Kind = SymbolKind.Property Then
                For Each conflict In {"_" + replacementText, "get_" + replacementText, "set_" + replacementText}
                    If Not possibleNameConflicts.Contains(conflict) Then
                        possibleNameConflicts.Add(conflict)
                    End If
                Next
            End If

            ' consider both versions of the identifier (escaped and unescaped)
            Dim valueText = replacementText
            Dim kind = SyntaxFacts.GetKeywordKind(replacementText)
            If kind <> SyntaxKind.None Then
                valueText = SyntaxFacts.GetText(kind)
            Else
                Dim name = SyntaxFactory.ParseName(replacementText)
                If name.VisualBasicKind = SyntaxKind.IdentifierName Then
                    valueText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                End If
            End If

            If CaseInsensitiveComparison.Compare(valueText, replacementText) <> 0 Then
                possibleNameConflicts.Add(valueText)
            End If
        End Sub

        Public Shared Function GetSemanticModelForNode(node As SyntaxNode, position As Integer, originalSemanticModel As SemanticModel) As SemanticModel
            If node.SyntaxTree Is originalSemanticModel.SyntaxTree Then
                ' This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                Return originalSemanticModel
            End If

            Dim syntax = node
            Dim nodeToSpeculate = syntax.GetAncestorsOrThis(Of SyntaxNode).Where(Function(n) SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault
            If nodeToSpeculate Is Nothing Then
                If syntax.IsKind(SyntaxKind.CrefReference) Then
                    nodeToSpeculate = DirectCast(syntax, CrefReferenceSyntax).Name
                Else
                    Return Nothing
                End If
            End If
            Dim isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(TryCast(syntax, ExpressionSyntax))
            Return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, DirectCast(originalSemanticModel, SemanticModel), position, isInNamespaceOrTypeContext)
        End Function

#End Region

    End Class

End Namespace