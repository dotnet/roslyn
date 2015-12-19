' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename

    Friend Class VisualBasicRenameRewriterLanguageService
        Implements IRenameRewriterLanguageService

        Private ReadOnly _languageServiceProvider As HostLanguageServices

        Public Sub New(provider As HostLanguageServices)
            _languageServiceProvider = provider
        End Sub

#Region "Annotate"

        Public Function AnnotateAndRename(parameters As RenameRewriterParameters) As SyntaxNode Implements IRenameRewriterLanguageService.AnnotateAndRename
            Dim renameRewriter = New RenameRewriter(parameters)
            Return renameRewriter.Visit(parameters.SyntaxRoot)
        End Function

        Private Class RenameRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _documentId As DocumentId
            Private ReadOnly _renameRenamableSymbolDeclaration As RenameAnnotation
            Private ReadOnly _solution As Solution
            Private ReadOnly _replacementText As String
            Private ReadOnly _originalText As String
            Private ReadOnly _possibleNameConflicts As ICollection(Of String)
            Private ReadOnly _renameLocations As Dictionary(Of TextSpan, RenameLocation)
            Private ReadOnly _conflictLocations As IEnumerable(Of TextSpan)
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _renamedSymbol As ISymbol
            Private ReadOnly _aliasSymbol As IAliasSymbol
            Private ReadOnly _renamableDeclarationLocation As Location
            Private ReadOnly _renameSpansTracker As RenamedSpansTracker
            Private ReadOnly _isVerbatim As Boolean
            Private ReadOnly _replacementTextValid As Boolean
            Private ReadOnly _isRenamingInStrings As Boolean
            Private ReadOnly _isRenamingInComments As Boolean
            Private ReadOnly _stringAndCommentTextSpans As ISet(Of TextSpan)
            Private ReadOnly _simplificationService As ISimplificationService
            Private ReadOnly _annotatedIdentifierTokens As New HashSet(Of SyntaxToken)
            Private ReadOnly _invocationExpressionsNeedingConflictChecks As New HashSet(Of InvocationExpressionSyntax)
            Private ReadOnly _syntaxFactsService As ISyntaxFactsService
            Private ReadOnly _semanticFactsService As ISemanticFactsService
            Private ReadOnly _renameAnnotations As AnnotationTable(Of RenameAnnotation)

            Private ReadOnly Property AnnotateForComplexification As Boolean
                Get
                    Return Me._skipRenameForComplexification > 0 AndAlso Not Me._isProcessingComplexifiedSpans
                End Get
            End Property

            Private _skipRenameForComplexification As Integer = 0
            Private _isProcessingComplexifiedSpans As Boolean
            Private _modifiedSubSpans As List(Of ValueTuple(Of TextSpan, TextSpan)) = Nothing
            Private _speculativeModel As SemanticModel
            Private _isProcessingStructuredTrivia As Integer
            Private ReadOnly _complexifiedSpans As HashSet(Of TextSpan) = New HashSet(Of TextSpan)

            Private Sub AddModifiedSpan(oldSpan As TextSpan, newSpan As TextSpan)
                newSpan = New TextSpan(oldSpan.Start, newSpan.Length)
                If Not Me._isProcessingComplexifiedSpans Then
                    _renameSpansTracker.AddModifiedSpan(_documentId, oldSpan, newSpan)
                Else
                    Me._modifiedSubSpans.Add(ValueTuple.Create(oldSpan, newSpan))
                End If
            End Sub

            Public Sub New(parameters As RenameRewriterParameters)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._documentId = parameters.Document.Id
                Me._renameRenamableSymbolDeclaration = parameters.RenamedSymbolDeclarationAnnotation
                Me._solution = parameters.OriginalSolution
                Me._replacementText = parameters.ReplacementText
                Me._originalText = parameters.OriginalText
                Me._possibleNameConflicts = parameters.PossibleNameConflicts
                Me._renameLocations = parameters.RenameLocations
                Me._conflictLocations = parameters.ConflictLocationSpans
                Me._cancellationToken = parameters.CancellationToken
                Me._semanticModel = DirectCast(parameters.SemanticModel, SemanticModel)
                Me._renamedSymbol = parameters.RenameSymbol
                Me._replacementTextValid = parameters.ReplacementTextValid
                Me._renameSpansTracker = parameters.RenameSpansTracker
                Me._isRenamingInStrings = parameters.OptionSet.GetOption(RenameOptions.RenameInStrings)
                Me._isRenamingInComments = parameters.OptionSet.GetOption(RenameOptions.RenameInComments)
                Me._stringAndCommentTextSpans = parameters.StringAndCommentTextSpans
                Me._aliasSymbol = TryCast(Me._renamedSymbol, IAliasSymbol)
                Me._renamableDeclarationLocation = Me._renamedSymbol.Locations.Where(Function(loc) loc.IsInSource AndAlso loc.SourceTree Is _semanticModel.SyntaxTree).FirstOrDefault()
                Me._simplificationService = parameters.Document.Project.LanguageServices.GetService(Of ISimplificationService)()
                Me._syntaxFactsService = parameters.Document.Project.LanguageServices.GetService(Of ISyntaxFactsService)()
                Me._semanticFactsService = parameters.Document.Project.LanguageServices.GetService(Of ISemanticFactsService)()
                Me._isVerbatim = Me._syntaxFactsService.IsVerbatimIdentifier(_replacementText)
                Me._renameAnnotations = parameters.RenameAnnotations
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is Nothing Then
                    Return node
                End If

                Dim isInConflictLambdaBody = False
                Dim lambdas = node.GetAncestorsOrThis(Of MultiLineLambdaExpressionSyntax)()
                If lambdas.Count() <> 0 Then
                    For Each lambda In lambdas
                        If Me._conflictLocations.Any(Function(cf)
                                                         Return cf.Contains(lambda.Span)
                                                     End Function) Then
                            isInConflictLambdaBody = True
                            Exit For
                        End If
                    Next
                End If

                Dim shouldComplexifyNode = Me.ShouldComplexifyNode(node, isInConflictLambdaBody)

                Dim result As SyntaxNode
                If shouldComplexifyNode Then
                    Me._skipRenameForComplexification += 1
                    result = MyBase.Visit(node)
                    Me._skipRenameForComplexification -= 1
                    result = Complexify(node, result)
                Else
                    result = MyBase.Visit(node)
                End If

                Return result
            End Function

            Private Function ShouldComplexifyNode(node As SyntaxNode, isInConflictLambdaBody As Boolean) As Boolean
                Return Not isInConflictLambdaBody AndAlso
                       _skipRenameForComplexification = 0 AndAlso
                       Not _isProcessingComplexifiedSpans AndAlso
                       _conflictLocations.Contains(node.Span) AndAlso
                       (TypeOf node Is ExpressionSyntax OrElse
                        TypeOf node Is StatementSyntax OrElse
                        TypeOf node Is AttributeSyntax OrElse
                        TypeOf node Is SimpleArgumentSyntax OrElse
                        TypeOf node Is CrefReferenceSyntax OrElse
                        TypeOf node Is TypeConstraintSyntax)
            End Function

            Private Function Complexify(originalNode As SyntaxNode, newNode As SyntaxNode) As SyntaxNode
                If Me._complexifiedSpans.Contains(originalNode.Span) Then
                    Return newNode
                Else
                    Me._complexifiedSpans.Add(originalNode.Span)
                End If

                Me._isProcessingComplexifiedSpans = True
                Me._modifiedSubSpans = New List(Of ValueTuple(Of TextSpan, TextSpan))()
                Dim annotation = New SyntaxAnnotation()

                newNode = newNode.WithAdditionalAnnotations(annotation)
                Dim speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode)
                newNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                Me._speculativeModel = GetSemanticModelForNode(newNode, Me._semanticModel)
                Debug.Assert(_speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")

                Dim oldSpan = originalNode.Span

                Dim expandParameter = originalNode.GetAncestorsOrThis(Of LambdaExpressionSyntax).Count() = 0

                Dim expandedNewNode = DirectCast(_simplificationService.Expand(newNode,
                                                                  _speculativeModel,
                                                                  annotationForReplacedAliasIdentifier:=Nothing,
                                                                  expandInsideNode:=AddressOf IsExpandWithinMultiLineLambda,
                                                                  expandParameter:=expandParameter,
                                                                  cancellationToken:=_cancellationToken), SyntaxNode)
                Dim annotationForSpeculativeNode = New SyntaxAnnotation()
                expandedNewNode = expandedNewNode.WithAdditionalAnnotations(annotationForSpeculativeNode)
                speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, expandedNewNode)
                Dim probableRenameNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                Dim speculativeNewNode = speculativeTree.GetAnnotatedNodes(Of SyntaxNode)(annotationForSpeculativeNode).First()

                Me._speculativeModel = GetSemanticModelForNode(speculativeNewNode, Me._semanticModel)
                Debug.Assert(_speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")
                Dim renamedNode = MyBase.Visit(probableRenameNode)

                If Not ReferenceEquals(renamedNode, probableRenameNode) Then
                    renamedNode = renamedNode.WithoutAnnotations(annotation)
                    probableRenameNode = expandedNewNode.GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
                    expandedNewNode = expandedNewNode.ReplaceNode(probableRenameNode, renamedNode)
                End If

                Dim newSpan = expandedNewNode.Span
                probableRenameNode = probableRenameNode.WithoutAnnotations(annotation)
                expandedNewNode = Me._renameAnnotations.WithAdditionalAnnotations(expandedNewNode, New RenameNodeSimplificationAnnotation() With {.OriginalTextSpan = oldSpan})

                Me._renameSpansTracker.AddComplexifiedSpan(Me._documentId, oldSpan, New TextSpan(oldSpan.Start, newSpan.Length), Me._modifiedSubSpans)
                Me._modifiedSubSpans = Nothing
                Me._isProcessingComplexifiedSpans = False
                Me._speculativeModel = Nothing
                Return expandedNewNode
            End Function

            Private Function IsExpandWithinMultiLineLambda(node As SyntaxNode) As Boolean
                If node Is Nothing Then
                    Return False
                End If

                If Me._conflictLocations.Contains(node.Span) Then
                    Return True
                End If

                If node.IsParentKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                node.IsParentKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then
                    Dim parent = DirectCast(node.Parent, MultiLineLambdaExpressionSyntax)
                    If ReferenceEquals(parent.SubOrFunctionHeader, node) Then
                        Return True
                    Else
                        Return False
                    End If
                End If

                Return True
            End Function

            Private Function IsPossibleNameConflict(possibleNameConflicts As ICollection(Of String), candidate As String) As Boolean
                For Each possibleNameConflict In possibleNameConflicts
                    If CaseInsensitiveComparison.Equals(possibleNameConflict, candidate) Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Private Function UpdateAliasAnnotation(newToken As SyntaxToken) As SyntaxToken
                If Me._aliasSymbol IsNot Nothing AndAlso Not Me.AnnotateForComplexification AndAlso newToken.HasAnnotations(AliasAnnotation.Kind) Then
                    newToken = RenameUtilities.UpdateAliasAnnotation(newToken, Me._aliasSymbol, Me._replacementText)
                End If

                Return newToken
            End Function

            Private Async Function RenameAndAnnotateAsync(token As SyntaxToken, newToken As SyntaxToken, isRenameLocation As Boolean, isOldText As Boolean) As Task(Of SyntaxToken)
                If Me._isProcessingComplexifiedSpans Then
                    If isRenameLocation Then
                        Dim annotation = Me._renameAnnotations.GetAnnotations(Of RenameActionAnnotation)(token).FirstOrDefault()
                        If annotation IsNot Nothing Then
                            newToken = RenameToken(token, newToken, annotation.Prefix, annotation.Suffix)
                            AddModifiedSpan(annotation.OriginalSpan, New TextSpan(token.Span.Start, newToken.Span.Length))
                        Else
                            newToken = RenameToken(token, newToken, prefix:=Nothing, suffix:=Nothing)
                        End If
                    End If

                    Return newToken
                End If

                Dim symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, Me._semanticModel, Me._solution.Workspace, Me._cancellationToken)

                ' this is the compiler generated backing field of a non custom event. We need to store a "Event" suffix to properly rename it later on.
                Dim prefix = If(isRenameLocation AndAlso Me._renameLocations(token.Span).IsRenamableAccessor, newToken.ValueText.Substring(0, newToken.ValueText.IndexOf("_"c) + 1), String.Empty)
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

                        If fieldSymbol.AssociatedSymbol IsNot Nothing AndAlso
                           fieldSymbol.AssociatedSymbol.IsKind(SymbolKind.Property) AndAlso
                           fieldSymbol.Name = "_" + fieldSymbol.AssociatedSymbol.Name Then

                            prefix = "_"
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

                    If Not isRenameLocation AndAlso TypeOf (symbol) Is INamespaceSymbol AndAlso token.GetPreviousToken().Kind = SyntaxKind.NamespaceKeyword Then
                        Return newToken
                    End If
                End If

                If isRenameLocation AndAlso Not Me.AnnotateForComplexification Then
                    Dim oldSpan = token.Span
                    newToken = RenameToken(token, newToken, prefix:=prefix, suffix:=suffix)
                    AddModifiedSpan(oldSpan, newToken.Span)
                End If

                Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                   Await ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(False)

                Dim isNamespaceDeclarationReference = False
                If isRenameLocation AndAlso token.GetPreviousToken().Kind = SyntaxKind.NamespaceKeyword Then
                    isNamespaceDeclarationReference = True
                End If

                Dim isMemberGroupReference = _semanticFactsService.IsNameOfContext(_semanticModel, token.Span.Start, _cancellationToken)

                Dim renameAnnotation = New RenameActionAnnotation(
                                    token.Span,
                                    isRenameLocation,
                                    prefix,
                                    suffix,
                                    isOldText,
                                    renameDeclarationLocations,
                                    isNamespaceDeclarationReference,
                                    isInvocationExpression:=False,
                                    isMemberGroupReference:=isMemberGroupReference)

                _annotatedIdentifierTokens.Add(token)
                newToken = Me._renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = token.Span})
                If Me._renameRenamableSymbolDeclaration IsNot Nothing AndAlso _renamableDeclarationLocation = token.GetLocation() Then
                    newToken = Me._renameAnnotations.WithAdditionalAnnotations(newToken, Me._renameRenamableSymbolDeclaration)
                End If

                Return newToken
            End Function

            Private Function IsInRenameLocation(token As SyntaxToken) As Boolean
                If Not Me._isProcessingComplexifiedSpans Then
                    Return Me._renameLocations.ContainsKey(token.Span)
                Else
                    If token.HasAnnotations(AliasAnnotation.Kind) Then
                        Return False
                    End If

                    If Me._renameAnnotations.HasAnnotations(Of RenameActionAnnotation)(token) Then
                        Return Me._renameAnnotations.GetAnnotations(Of RenameActionAnnotation)(token).First().IsRenameLocation
                    End If

                    If TypeOf token.Parent Is SimpleNameSyntax AndAlso token.Kind <> SyntaxKind.GlobalKeyword AndAlso token.Parent.Parent.IsKind(SyntaxKind.QualifiedName, SyntaxKind.QualifiedCrefOperatorReference) Then
                        Dim symbol = Me._speculativeModel.GetSymbolInfo(token.Parent, Me._cancellationToken).Symbol
                        If symbol IsNot Nothing AndAlso Me._renamedSymbol.Kind <> SymbolKind.Local AndAlso Me._renamedSymbol.Kind <> SymbolKind.RangeVariable AndAlso
                            (symbol Is Me._renamedSymbol OrElse SymbolKey.GetComparer(ignoreCase:=True, ignoreAssemblyKeys:=False).Equals(symbol.GetSymbolKey(), Me._renamedSymbol.GetSymbolKey())) Then
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
                Dim shouldCheckTrivia = Me._stringAndCommentTextSpans.Contains(oldToken.Span)
                If shouldCheckTrivia Then
                    Me._isProcessingStructuredTrivia += 1
                    newToken = MyBase.VisitToken(newToken)
                    Me._isProcessingStructuredTrivia -= 1
                Else
                    newToken = MyBase.VisitToken(newToken)
                End If

                newToken = UpdateAliasAnnotation(newToken)

                ' Rename matches in strings and comments
                newToken = RenameWithinToken(oldToken, newToken)

                ' We don't want to annotate XmlName with RenameActionAnnotation
                If newToken.Kind = SyntaxKind.XmlNameToken Then
                    Return newToken
                End If

                Dim isRenameLocation = IsInRenameLocation(oldToken)
                Dim isOldText = CaseInsensitiveComparison.Equals(oldToken.ValueText, _originalText)
                Dim tokenNeedsConflictCheck = isRenameLocation OrElse
                    isOldText OrElse
                    CaseInsensitiveComparison.Equals(oldToken.ValueText, _replacementText) OrElse
                    IsPossibleNameConflict(_possibleNameConflicts, oldToken.ValueText)

                If tokenNeedsConflictCheck Then
                    newToken = RenameAndAnnotateAsync(oldToken, newToken, isRenameLocation, isOldText).WaitAndGetResult_CanCallOnBackground(_cancellationToken)

                    If Not Me._isProcessingComplexifiedSpans Then
                        _invocationExpressionsNeedingConflictChecks.AddRange(oldToken.GetAncestors(Of InvocationExpressionSyntax)())
                    End If
                End If

                Return newToken
            End Function

            Private Function GetAnnotationForInvocationExpression(invocationExpression As InvocationExpressionSyntax) As RenameActionAnnotation
                Dim identifierToken As SyntaxToken = Nothing
                Dim expressionOfInvocation = invocationExpression.Expression
                While expressionOfInvocation IsNot Nothing
                    Select Case expressionOfInvocation.Kind
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
                        Case Else
                            ' This isn't actually an invocation, so there's no member name to check.
                            Return Nothing
                    End Select
                End While

                If identifierToken <> Nothing AndAlso Not Me._annotatedIdentifierTokens.Contains(identifierToken) Then
                    Dim symbolInfo = Me._semanticModel.GetSymbolInfo(invocationExpression, Me._cancellationToken)
                    Dim symbols As IEnumerable(Of ISymbol) = Nothing
                    If symbolInfo.Symbol Is Nothing Then
                        Return Nothing
                    Else
                        symbols = SpecializedCollections.SingletonEnumerable(symbolInfo.Symbol)
                    End If

                    Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                        ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).WaitAndGetResult_CanCallOnBackground(_cancellationToken)

                    Dim renameAnnotation = New RenameActionAnnotation(
                                            identifierToken.Span,
                                            isRenameLocation:=False,
                                            prefix:=Nothing,
                                            suffix:=Nothing,
                                            renameDeclarationLocations:=renameDeclarationLocations,
                                            isOriginalTextLocation:=False,
                                            isNamespaceDeclarationReference:=False,
                                            isInvocationExpression:=True,
                                            isMemberGroupReference:=False)

                    Return renameAnnotation
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                Dim result = MyBase.VisitInvocationExpression(node)
                If _invocationExpressionsNeedingConflictChecks.Contains(node) Then
                    Dim renameAnnotation = GetAnnotationForInvocationExpression(node)
                    If renameAnnotation IsNot Nothing Then
                        result = Me._renameAnnotations.WithAdditionalAnnotations(result, renameAnnotation)
                    End If
                End If

                Return result
            End Function

            Private Function RenameToken(oldToken As SyntaxToken, newToken As SyntaxToken, prefix As String, suffix As String) As SyntaxToken
                Dim parent = oldToken.Parent
                Dim currentNewIdentifier = Me._replacementText
                Dim oldIdentifier = newToken.ValueText
                Dim isAttributeName = SyntaxFacts.IsAttributeName(parent)
                If isAttributeName Then
                    Debug.Assert(Me._renamedSymbol.IsAttribute() OrElse Me._aliasSymbol.Target.IsAttribute())
                    If oldIdentifier <> Me._renamedSymbol.Name Then
                        Dim withoutSuffix = String.Empty
                        If currentNewIdentifier.TryReduceAttributeSuffix(withoutSuffix) Then
                            currentNewIdentifier = withoutSuffix
                        End If
                    End If
                Else
                    If Not String.IsNullOrEmpty(prefix) Then
                        currentNewIdentifier = prefix + currentNewIdentifier
                    End If

                    If Not String.IsNullOrEmpty(suffix) Then
                        currentNewIdentifier = currentNewIdentifier + suffix
                    End If
                End If

                ' determine the canonical identifier name (unescaped, no type char, ...)
                Dim valueText = currentNewIdentifier
                Dim name = SyntaxFactory.ParseName(currentNewIdentifier)
                If name.ContainsDiagnostics Then
                    name = SyntaxFactory.IdentifierName(currentNewIdentifier)
                End If

                If name.IsKind(SyntaxKind.GlobalName) Then
                    valueText = currentNewIdentifier
                ElseIf name.IsKind(SyntaxKind.IdentifierName) Then
                    valueText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                End If

                If Me._isVerbatim Then
                    newToken = newToken.CopyAnnotationsTo(SyntaxFactory.BracketedIdentifier(newToken.LeadingTrivia, valueText, newToken.TrailingTrivia))
                Else
                    newToken = newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(
                                                          newToken.LeadingTrivia,
                                                          If(oldToken.GetTypeCharacter() = TypeCharacter.None, currentNewIdentifier, currentNewIdentifier + oldToken.ToString().Last()),
                                                          False,
                                                          valueText,
                                                      oldToken.GetTypeCharacter(),
                                                          newToken.TrailingTrivia))

                    If Me._replacementTextValid AndAlso
                        oldToken.GetTypeCharacter() <> TypeCharacter.None AndAlso
                        (SyntaxFacts.GetKeywordKind(valueText) = SyntaxKind.REMKeyword OrElse Me._syntaxFactsService.IsVerbatimIdentifier(newToken)) Then

                        newToken = Me._renameAnnotations.WithAdditionalAnnotations(newToken, RenameInvalidIdentifierAnnotation.Instance)
                    End If
                End If

                If Me._replacementTextValid Then
                    If newToken.IsBracketed Then
                        ' a reference location should always be tried to be unescaped, whether it was escaped before rename 
                        ' or the replacement itself is escaped.
                        newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation)
                    Else
                        Dim semanticModel = GetSemanticModelForNode(parent, If(Me._speculativeModel, Me._semanticModel))
                        newToken = Simplification.VisualBasicSimplificationService.TryEscapeIdentifierToken(newToken, semanticModel, oldToken)
                    End If
                End If

                Return newToken
            End Function

            Private Function RenameInStringLiteral(oldToken As SyntaxToken, newToken As SyntaxToken, createNewStringLiteral As Func(Of SyntaxTriviaList, String, String, SyntaxTriviaList, SyntaxToken)) As SyntaxToken
                Dim originalString = newToken.ToString()
                Dim replacedString As String = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, _originalText, _replacementText)
                If replacedString <> originalString Then
                    Dim oldSpan = oldToken.Span
                    newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia)
                    AddModifiedSpan(oldSpan, newToken.Span)
                    Return newToken.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
                End If

                Return newToken
            End Function

            Private Function RenameInCommentTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim originalString = trivia.ToString()
                Dim replacedString As String = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, _originalText, _replacementText)
                If replacedString <> originalString Then
                    Dim oldSpan = trivia.Span
                    Dim newTrivia = SyntaxFactory.CommentTrivia(replacedString)
                    AddModifiedSpan(oldSpan, newTrivia.Span)
                    Return trivia.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newTrivia, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
                End If

                Return trivia
            End Function

            Private Function RenameInTrivia(token As SyntaxToken, leadingOrTrailingTriviaList As IEnumerable(Of SyntaxTrivia)) As SyntaxToken
                Return token.ReplaceTrivia(leadingOrTrailingTriviaList, Function(oldTrivia, newTrivia)
                                                                            If newTrivia.Kind = SyntaxKind.CommentTrivia Then
                                                                                Return RenameInCommentTrivia(newTrivia)
                                                                            End If

                                                                            Return newTrivia
                                                                        End Function)
            End Function

            Private Function RenameWithinToken(oldToken As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
                If Me._isProcessingComplexifiedSpans OrElse
                (Me._isProcessingStructuredTrivia = 0 AndAlso Not Me._stringAndCommentTextSpans.Contains(oldToken.Span)) Then
                    Return newToken
                End If

                If Me._isRenamingInStrings Then
                    If newToken.Kind = SyntaxKind.StringLiteralToken Then
                        newToken = RenameInStringLiteral(oldToken, newToken, AddressOf SyntaxFactory.StringLiteralToken)
                    ElseIf newToken.Kind = SyntaxKind.InterpolatedStringTextToken Then
                        newToken = RenameInStringLiteral(oldToken, newToken, AddressOf SyntaxFactory.InterpolatedStringTextToken)
                    End If
                End If

                If Me._isRenamingInComments Then
                    If newToken.Kind = SyntaxKind.XmlTextLiteralToken Then
                        newToken = RenameInStringLiteral(oldToken, newToken, AddressOf SyntaxFactory.XmlTextLiteralToken)
                    ElseIf newToken.Kind = SyntaxKind.XmlNameToken AndAlso CaseInsensitiveComparison.Equals(oldToken.ValueText, _originalText) Then
                        Dim newIdentifierToken = SyntaxFactory.XmlNameToken(newToken.LeadingTrivia, _replacementText, SyntaxFacts.GetKeywordKind(_replacementText), newToken.TrailingTrivia)
                        newToken = newToken.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldToken.Span}))
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
            token As SyntaxToken,
            newReferencedSymbols As IEnumerable(Of ISymbol)
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
                    CaseInsensitiveComparison.Equals(renamedSymbol.ContainingSymbol.Name, renamedSymbol.Name) Then

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
                    If typeParameter IsNot currentTypeParameter AndAlso CaseInsensitiveComparison.Equals(token.ValueText, typeParameter.Identifier.ValueText) Then
                        conflicts.Add(reverseMappedLocations(typeParameter.Identifier.GetLocation()))
                    End If
                Next
            End If

            ' if the renamed symbol is a type member, it's name should not conflict with a type parameter
            If renamedSymbol.ContainingType IsNot Nothing AndAlso renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol) Then
                For Each typeParameter In renamedSymbol.ContainingType.TypeParameters
                    If CaseInsensitiveComparison.Equals(typeParameter.Name, renamedSymbol.Name) Then
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
                                                CaseInsensitiveComparison.Equals(renameSymbol.Name, "Current")
            implicitReferencesMightConflict = implicitReferencesMightConflict OrElse
                                                (renameSymbol.Kind = SymbolKind.Method AndAlso
                                                    (CaseInsensitiveComparison.Equals(renameSymbol.Name, "MoveNext") OrElse
                                                    CaseInsensitiveComparison.Equals(renameSymbol.Name, "GetEnumerator")))

            ' TODO: handle Dispose for using statement and Add methods for collection initializers.

            If implicitReferencesMightConflict Then
                If Not CaseInsensitiveComparison.Equals(renamedSymbol.Name, renameSymbol.Name) Then
                    For Each implicitReferenceLocation In implicitReferenceLocations
                        Dim token = implicitReferenceLocation.Location.SourceTree.GetTouchingToken(implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, False)

                        If token.Kind = SyntaxKind.ForKeyword AndAlso token.Parent.IsKind(SyntaxKind.ForEachStatement) Then
                            Return SpecializedCollections.SingletonEnumerable(DirectCast(token.Parent, ForEachStatementSyntax).Expression.GetLocation())
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
        Public Function GetExpansionTargetForLocation(token As SyntaxToken) As SyntaxNode Implements IRenameRewriterLanguageService.GetExpansionTargetForLocation
            Return GetExpansionTarget(token)
        End Function

        Private Shared Function GetExpansionTarget(token As SyntaxToken) As SyntaxNode
            ' get the directly enclosing statement
            Dim enclosingStatement = token.FirstAncestorOrSelf(Function(n) TypeOf (n) Is ExecutableStatementSyntax)

            ' for nodes in a using, for or for each statement, we do not need the enclosing _executable_ statement, which is the whole block.
            ' it's enough to expand the using, for or foreach statement.
            Dim possibleSpecialStatement = token.FirstAncestorOrSelf(Function(n) n.Kind = SyntaxKind.ForStatement OrElse
                                                                                 n.Kind = SyntaxKind.ForEachStatement OrElse
                                                                                 n.Kind = SyntaxKind.UsingStatement OrElse
                                                                                 n.Kind = SyntaxKind.CatchBlock)
            If possibleSpecialStatement IsNot Nothing Then
                If enclosingStatement Is possibleSpecialStatement.Parent Then
                    enclosingStatement = If(possibleSpecialStatement.Kind = SyntaxKind.CatchBlock,
                                                DirectCast(possibleSpecialStatement, CatchBlockSyntax).CatchStatement,
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
                If replacementText.StartsWith("[", StringComparison.Ordinal) AndAlso replacementText.EndsWith("]", StringComparison.Ordinal) Then
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
            If CaseInsensitiveComparison.Equals(renamedSymbol.Name, "MoveNext") OrElse
                    CaseInsensitiveComparison.Equals(renamedSymbol.Name, "GetEnumerator") OrElse
                    CaseInsensitiveComparison.Equals(renamedSymbol.Name, "Current") Then


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

                            If CaseInsensitiveComparison.Equals(symbol.Name, "MoveNext") Then
                                If Not method.ReturnsVoid AndAlso Not method.Parameters.Any() AndAlso method.ReturnType.SpecialType = SpecialType.System_Boolean Then
                                    Return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation)
                                End If
                            ElseIf CaseInsensitiveComparison.Equals(symbol.Name, "GetEnumerator") Then
                                ' we are a bit pessimistic here. 
                                ' To be sure we would need to check if the returned type Is having a MoveNext And Current as required by foreach
                                If Not method.ReturnsVoid AndAlso
                                        Not method.Parameters.Any() Then
                                    Return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation)
                                End If
                            End If

                        ElseIf CaseInsensitiveComparison.Equals(symbol.Name, "Current") Then
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

            If replacementText.Length > AttributeSuffixLength AndAlso CaseInsensitiveComparison.Equals(halfWidthReplacementText.Substring(halfWidthReplacementText.Length - AttributeSuffixLength), AttributeSuffix) Then
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
                If name.Kind = SyntaxKind.IdentifierName Then
                    valueText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                End If
            End If

            If Not CaseInsensitiveComparison.Equals(valueText, replacementText) Then
                possibleNameConflicts.Add(valueText)
            End If
        End Sub

        ''' <summary>
        ''' Gets the semantic model for the given node. 
        ''' If the node belongs to the syntax tree of the original semantic model, then returns originalSemanticModel.
        ''' Otherwise, returns a speculative model.
        ''' The assumption for the later case is that span start position of the given node in it's syntax tree is same as
        ''' the span start of the original node in the original syntax tree.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <param name="originalSemanticModel"></param>
        Public Shared Function GetSemanticModelForNode(node As SyntaxNode, originalSemanticModel As SemanticModel) As SemanticModel
            If node.SyntaxTree Is originalSemanticModel.SyntaxTree Then
                ' This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                Return originalSemanticModel
            End If

            Dim syntax = node
            Dim nodeToSpeculate = syntax.GetAncestorsOrThis(Of SyntaxNode).Where(Function(n) SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault
            If nodeToSpeculate Is Nothing Then
                If syntax.IsKind(SyntaxKind.CrefReference) Then
                    nodeToSpeculate = DirectCast(syntax, CrefReferenceSyntax).Name
                ElseIf syntax.IsKind(SyntaxKind.TypeConstraint) Then
                    nodeToSpeculate = DirectCast(syntax, TypeConstraintSyntax).Type
                Else
                    Return Nothing
                End If
            End If
            Dim isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(TryCast(syntax, ExpressionSyntax))
            Dim position = nodeToSpeculate.SpanStart
            Return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, DirectCast(originalSemanticModel, SemanticModel), position, isInNamespaceOrTypeContext)
        End Function

#End Region

    End Class

End Namespace
