' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    Friend Class SymbolsRenameRewriter
        Inherits VisualBasicSyntaxRewriter

        Private ReadOnly _textSpanToRenameContexts As Dictionary(Of TextSpan, TextSpanRenameContext)
        Private ReadOnly _stringAndCommentRenameContexts As Dictionary(Of TextSpan, HashSet(Of TextSpanRenameContext))
        Private ReadOnly _renameContexts As Dictionary(Of SymbolKey, RenameSymbolContext)

        Private ReadOnly _documentId As DocumentId
        Private ReadOnly _solution As Solution
        Private ReadOnly _conflictLocations As ISet(Of TextSpan)
        Private ReadOnly _semanticModel As SemanticModel
        Private ReadOnly _cancellationToken As CancellationToken
        Private ReadOnly _renameSpansTracker As RenamedSpansTracker
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

        Private _skipRenameForComplexification As Integer
        Private _isProcessingComplexifiedSpans As Boolean
        Private _modifiedSubSpans As List(Of (TextSpan, TextSpan))
        Private _speculativeModel As SemanticModel

        Private ReadOnly _complexifiedSpans As HashSet(Of TextSpan) = New HashSet(Of TextSpan)

        Private Sub AddModifiedSpan(oldSpan As TextSpan, newSpan As TextSpan)
            newSpan = New TextSpan(oldSpan.Start, newSpan.Length)
            If Not Me._isProcessingComplexifiedSpans Then
                _renameSpansTracker.AddModifiedSpan(_documentId, oldSpan, newSpan)
            Else
                Me._modifiedSubSpans.Add((oldSpan, newSpan))
            End If
        End Sub

        Public Sub New(parameters As RenameRewriterParameters)
            MyBase.New(visitIntoStructuredTrivia:=True)
            _documentId = parameters.Document.Id
            _solution = parameters.OriginalSolution
            _conflictLocations = parameters.ConflictLocationSpans
            _cancellationToken = parameters.CancellationToken
            _semanticModel = parameters.SemanticModel
            _simplificationService = parameters.Document.Project.LanguageServices.GetRequiredService(Of ISimplificationService)()
            _syntaxFactsService = parameters.Document.Project.LanguageServices.GetRequiredService(Of ISyntaxFactsService)()
            _semanticFactsService = parameters.Document.Project.LanguageServices.GetRequiredService(Of ISemanticFactsService)()
            _renameAnnotations = parameters.RenameAnnotations
            _renameSpansTracker = parameters.RenameSpansTracker

            _renameContexts = RenameUtilities.GroupRenameContextBySymbolKey(parameters.RenameSymbolContexts, SymbolKey.GetComparer(ignoreCase:=True, ignoreAssemblyKeys:=False))
            _textSpanToRenameContexts = RenameUtilities.GroupTextRenameContextsByTextSpan(parameters.TokenTextSpanRenameContexts)
            _stringAndCommentRenameContexts = RenameUtilities.GroupStringAndCommentsTextSpanRenameContexts(parameters.StringAndCommentsTextSpanRenameContexts)
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
            Me._speculativeModel = VisualBasicRenameRewriterLanguageService.GetSemanticModelForNode(newNode, Me._semanticModel)
            Debug.Assert(_speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")

            ' There are cases when we change the type of node to make speculation work (e.g.,
            ' for AsNewClauseSyntax), so getting the newNode from the _speculativeModel 
            ' ensures the final node replacing the original node is found.
            newNode = Me._speculativeModel.SyntaxTree.GetRoot(_cancellationToken).GetAnnotatedNodes(Of SyntaxNode)(annotation).First()

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

            Me._speculativeModel = VisualBasicRenameRewriterLanguageService.GetSemanticModelForNode(speculativeNewNode, Me._semanticModel)
            Debug.Assert(_speculativeModel IsNot Nothing, "expanding a syntax node which cannot be speculated?")

            ' There are cases when we change the type of node to make speculation work (e.g.,
            ' for AsNewClauseSyntax), so getting the newNode from the _speculativeModel 
            ' ensures the final node replacing the original node is found.
            probableRenameNode = Me._speculativeModel.SyntaxTree.GetRoot(_cancellationToken).GetAnnotatedNodes(Of SyntaxNode)(annotation).First()
            speculativeNewNode = Me._speculativeModel.SyntaxTree.GetRoot(_cancellationToken).GetAnnotatedNodes(Of SyntaxNode)(annotationForSpeculativeNode).First()

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

        Private Shared Function IsPossibleNameConflict(possibleNameConflicts As ICollection(Of String), candidate As String) As Boolean
            For Each possibleNameConflict In possibleNameConflicts
                If CaseInsensitiveComparison.Equals(possibleNameConflict, candidate) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Function UpdateAliasAnnotation(newToken As SyntaxToken) As SyntaxToken

            For Each kvp In _renameContexts
                Dim renameSymbolContext = kvp.Value
                Dim aliasSymbol = renameSymbolContext.AliasSymbol
                If aliasSymbol IsNot Nothing AndAlso Not Me.AnnotateForComplexification AndAlso newToken.HasAnnotations(AliasAnnotation.Kind) Then
                    newToken = RenameUtilities.UpdateAliasAnnotation(newToken, aliasSymbol, renameSymbolContext.ReplacementText)
                End If
            Next

            Return newToken
        End Function

        Private Async Function AnnotateForConflictCheckAsync(token As SyntaxToken, isOldText As Boolean) As Task(Of SyntaxToken)
            If token.IsKind(SyntaxKind.NewKeyword) Then
                ' The constructor definition cannot be renamed in Visual Basic
                Return token
            End If

            Dim symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Workspace.Services, _cancellationToken)
            Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                   Await ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(False)

            Dim isMemberGroupReference = token.Parent IsNot Nothing AndAlso _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken)

            Dim isNamespaceDeclarationReference = False
            If token.GetPreviousToken().Kind = SyntaxKind.NamespaceKeyword Then
                isNamespaceDeclarationReference = True
            End If

            Dim renameAnnotation = New RenameActionAnnotation(
                                    token.Span,
                                    isRenameLocation:=False,
                                    Nothing,
                                    Nothing,
                                    isOldText,
                                    renameDeclarationLocations,
                                    isNamespaceDeclarationReference:=isNamespaceDeclarationReference,
                                    isInvocationExpression:=False,
                                    isMemberGroupReference:=isMemberGroupReference)

            _annotatedIdentifierTokens.Add(token)
            Dim newToken = Me._renameAnnotations.WithAdditionalAnnotations(token, renameAnnotation, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = token.Span})
            Return newToken
        End Function

        Private Async Function RenameAndAnnotateAsync(token As SyntaxToken, newToken As SyntaxToken, isRenameLocation As Boolean, isOldText As Boolean, renameSymbolContext As TextSpanRenameContext) As Task(Of SyntaxToken)
            If newToken.IsKind(SyntaxKind.NewKeyword) Then
                ' The constructor definition cannot be renamed in Visual Basic
                Return newToken
            End If

            Dim replacementText = renameSymbolContext.SymbolContext.ReplacementText
            Dim replacementTextValid = renameSymbolContext.SymbolContext.ReplacementTextValid
            Dim renamedSymbol = renameSymbolContext.SymbolContext.RenamedSymbol
            Dim aliasSymbol = renameSymbolContext.SymbolContext.AliasSymbol
            Dim isVerbatim = _syntaxFactsService.IsVerbatimIdentifier(replacementText)

            If Me._isProcessingComplexifiedSpans Then
                If isRenameLocation Then
                    Dim annotation = Me._renameAnnotations.GetAnnotations(Of RenameActionAnnotation)(token).FirstOrDefault()
                    If annotation IsNot Nothing Then
                        newToken = RenameToken(
                                token,
                                newToken,
                                annotation.Prefix,
                                annotation.Suffix,
                                isVerbatim,
                                replacementText,
                                replacementTextValid,
                                renamedSymbol,
                                aliasSymbol)

                        AddModifiedSpan(annotation.OriginalSpan, New TextSpan(token.Span.Start, newToken.Span.Length))
                    Else
                        newToken = RenameToken(token, newToken, prefix:=Nothing, suffix:=Nothing, isVerbatim, replacementText, replacementTextValid, renamedSymbol, aliasSymbol)
                    End If
                End If

                Return newToken
            End If

            Dim symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Workspace.Services, _cancellationToken)

            ' this is the compiler generated backing field of a non custom event. We need to store a "Event" suffix to properly rename it later on.
            Dim prefix = If(isRenameLocation AndAlso renameSymbolContext.RenameLocation.IsRenamableAccessor, newToken.ValueText.Substring(0, newToken.ValueText.IndexOf("_"c) + 1), String.Empty)
            Dim suffix As String = Nothing

            If symbols.Length = 1 Then
                Dim symbol = symbols(0)

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
                newToken = RenameToken(token, newToken, prefix:=prefix, suffix:=suffix, isVerbatim, replacementText, replacementTextValid, renamedSymbol, aliasSymbol)
                AddModifiedSpan(oldSpan, newToken.Span)
            End If

            Dim renameDeclarationLocations As RenameDeclarationLocationReference() =
                   Await ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(False)

            Dim isNamespaceDeclarationReference = False
            If isRenameLocation AndAlso token.GetPreviousToken().Kind = SyntaxKind.NamespaceKeyword Then
                isNamespaceDeclarationReference = True
            End If

            Dim isMemberGroupReference = _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken)

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

            'Dim declarationLocation = renameSymbolContext.RenamableDeclarationLocation
            'If declarationLocation IsNot Nothing AndAlso declarationLocation = token.GetLocation() Then
            '    newToken = Me._renameAnnotations.WithAdditionalAnnotations(newToken, renameSymbolContext.RenameRenamableSymbolDeclarationAnnotation)
            'End If

            Return newToken
        End Function

        Private Function TryFindSymbolContextForComplexifiedToken(token As SyntaxToken, ByRef renameSymbolContext As RenameSymbolContext) As Boolean
            If _isProcessingComplexifiedSpans Then
                RoslynDebug.Assert(_speculativeModel IsNot Nothing)

                If TypeOf token.Parent Is SimpleNameSyntax AndAlso token.Kind <> SyntaxKind.GlobalKeyword AndAlso token.Parent.Parent.IsKind(SyntaxKind.QualifiedName, SyntaxKind.QualifiedCrefOperatorReference) Then
                    Dim symbol = Me._speculativeModel.GetSymbolInfo(token.Parent, Me._cancellationToken).Symbol
                    If symbol IsNot Nothing AndAlso
                        _renameContexts.TryGetValue(symbol.GetSymbolKey(), renameSymbolContext) AndAlso
                        renameSymbolContext.RenamedSymbol.Kind <> SymbolKind.Local AndAlso
                        renameSymbolContext.RenamedSymbol.Kind <> SymbolKind.RangeVariable AndAlso
                        token.ValueText = renameSymbolContext.OriginalText Then
                        Return True
                    End If
                End If
            End If

            renameSymbolContext = Nothing
            Return False
        End Function

        Private Function TryGetLocationRenameContext(token As SyntaxToken, ByRef textSpanRenameContext As TextSpanRenameContext) As Boolean
            If Not _isProcessingComplexifiedSpans Then
                Return _textSpanToRenameContexts.TryGetValue(token.Span, textSpanRenameContext)
            Else

                If token.HasAnnotations(AliasAnnotation.Kind) Then
                    Return False
                End If

                If Not token.HasAnnotations(RenameAnnotation.Kind) Then
                    Return False
                End If

                Dim annotation = _renameAnnotations.GetAnnotations(token).OfType(Of RenameActionAnnotation).SingleOrDefault(
                        Function(renameActionAnnotation) renameActionAnnotation.IsRenameLocation)

                Return annotation IsNot Nothing AndAlso _textSpanToRenameContexts.TryGetValue(annotation.OriginalSpan, textSpanRenameContext)
            End If

            Return False
        End Function

        Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            Dim newTrivia = MyBase.VisitTrivia(trivia)

            Dim textSpanRenameContexts As HashSet(Of TextSpanRenameContext) = Nothing
            If Not trivia.HasStructure AndAlso _stringAndCommentRenameContexts.TryGetValue(trivia.Span, textSpanRenameContexts) Then
                Dim subSpanToReplacement = RenameUtilities.CreateSubSpanToReplacementTextDictionary(textSpanRenameContexts)
                Return RenameInCommentTrivia(trivia, subSpanToReplacement)
            End If

            Return newTrivia
        End Function

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If token = Nothing Then
                Return token
            End If

            Dim newToken = MyBase.VisitToken(token)
            newToken = UpdateAliasAnnotation(newToken)
            newToken = RenameTokenInStringOrComment(token, newToken)

            Dim textSpanRenameContext As TextSpanRenameContext = Nothing
            If Not _isProcessingComplexifiedSpans AndAlso TryGetLocationRenameContext(token, textSpanRenameContext) Then
                newToken = RenameAndAnnotateAsync(token, newToken, isRenameLocation:=True, isOldText:=False, textSpanRenameContext).WaitAndGetResult_CanCallOnBackground(_cancellationToken)
                _invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors(Of InvocationExpressionSyntax)())
                Return newToken
            End If

            If _isProcessingComplexifiedSpans Then
                Dim textSpanContextForComplexifiedToken As TextSpanRenameContext = Nothing
                If TryGetLocationRenameContext(token, textSpanContextForComplexifiedToken) Then
                    Return RenameComplexifiedToken(token, newToken, textSpanContextForComplexifiedToken)
                End If

                Dim renameSymbolContext As RenameSymbolContext = Nothing
                If TryFindSymbolContextForComplexifiedToken(token, renameSymbolContext) Then
                    Return RenameComplexifiedToken(token, newToken, renameSymbolContext)
                End If
            End If

            Return AnnotateNonRenameLocation(token, newToken)
        End Function

        Private Function RenameComplexifiedToken(token As SyntaxToken, newToken As SyntaxToken, textSpanRenameContext As TextSpanRenameContext) As SyntaxToken
            If _isProcessingComplexifiedSpans Then
                Dim annotation = _renameAnnotations.GetAnnotations(token).OfType(Of RenameActionAnnotation)().FirstOrDefault()

                newToken = RenameToken(
                            token,
                            newToken,
                            annotation.Prefix,
                            annotation.Suffix,
                            _syntaxFactsService.IsVerbatimIdentifier(textSpanRenameContext.SymbolContext.ReplacementText),
                            textSpanRenameContext.SymbolContext.ReplacementText,
                            textSpanRenameContext.SymbolContext.ReplacementTextValid,
                            textSpanRenameContext.SymbolContext.RenamedSymbol,
                            textSpanRenameContext.SymbolContext.AliasSymbol)

                AddModifiedSpan(annotation.OriginalSpan, newToken.Span)
            End If

            Return newToken
        End Function

        Private Function RenameComplexifiedToken(token As SyntaxToken, newToken As SyntaxToken, renameSymbolContext As RenameSymbolContext) As SyntaxToken
            If _isProcessingComplexifiedSpans Then
                Return RenameToken(
                            token,
                            newToken,
                            prefix:=Nothing,
                            suffix:=Nothing,
                            _syntaxFactsService.IsVerbatimIdentifier(renameSymbolContext.ReplacementText),
                            renameSymbolContext.ReplacementText,
                            renameSymbolContext.ReplacementTextValid,
                            renameSymbolContext.RenamedSymbol,
                            renameSymbolContext.AliasSymbol)
            End If

            Return newToken
        End Function


        Private Function AnnotateNonRenameLocation(oldToken As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
            If Not _isProcessingComplexifiedSpans Then
                newToken = UpdateAliasAnnotation(newToken)
                Dim renameContexts = _renameContexts.Values
                Dim tokenText = oldToken.ValueText
                Dim replacementMatchedContexts = RenameUtilities.GetMatchedContexts(renameContexts, Function(c) CaseInsensitiveComparison.Equals(tokenText, c.ReplacementText))
                Dim originalTextMatchedContexts = RenameUtilities.GetMatchedContexts(renameContexts, Function(c) CaseInsensitiveComparison.Equals(tokenText, c.OriginalText))
                Dim possibleNameConflictContexts = RenameUtilities.GetMatchedContexts(renameContexts, Function(c) IsPossibleNameConflict(c.PossibleNameConflicts, tokenText))

                If Not replacementMatchedContexts.IsEmpty OrElse Not originalTextMatchedContexts.IsEmpty OrElse Not possibleNameConflictContexts.IsEmpty Then
                    newToken = AnnotateForConflictCheckAsync(newToken, Not originalTextMatchedContexts.IsEmpty).WaitAndGetResult_CanCallOnBackground(_cancellationToken)
                    _invocationExpressionsNeedingConflictChecks.AddRange(oldToken.GetAncestors(Of InvocationExpressionSyntax)())
                End If

                Return newToken
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
                Dim symbols As IEnumerable(Of ISymbol)
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

        Private Function RenameToken(
                    oldToken As SyntaxToken,
                    newToken As SyntaxToken,
                    prefix As String,
                    suffix As String,
                    isReplacementTextVerbatim As Boolean,
                    replacementText As String,
                    isReplacementTextValid As Boolean,
                    renamedSymbol As ISymbol,
                    aliasSymbol As IAliasSymbol) As SyntaxToken

            Dim parent = oldToken.Parent
            Dim currentNewIdentifier = replacementText
            Dim oldIdentifier = newToken.ValueText
            Dim isAttributeName = SyntaxFacts.IsAttributeName(parent)
            If isAttributeName Then
                Debug.Assert(renamedSymbol.IsAttribute() OrElse aliasSymbol.Target.IsAttribute())
                If oldIdentifier <> renamedSymbol.Name Then
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

            If isReplacementTextVerbatim Then
                newToken = newToken.CopyAnnotationsTo(SyntaxFactory.BracketedIdentifier(newToken.LeadingTrivia, valueText, newToken.TrailingTrivia))
            Else
                newToken = newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(
                                                          newToken.LeadingTrivia,
                                                          If(oldToken.GetTypeCharacter() = TypeCharacter.None, currentNewIdentifier, currentNewIdentifier + oldToken.ToString().Last()),
                                                          False,
                                                          valueText,
                                                      oldToken.GetTypeCharacter(),
                                                          newToken.TrailingTrivia))

                If isReplacementTextValid AndAlso
                        oldToken.GetTypeCharacter() <> TypeCharacter.None AndAlso
                        (SyntaxFacts.GetKeywordKind(valueText) = SyntaxKind.REMKeyword OrElse Me._syntaxFactsService.IsVerbatimIdentifier(newToken)) Then

                    newToken = Me._renameAnnotations.WithAdditionalAnnotations(newToken, RenameInvalidIdentifierAnnotation.Instance)
                End If
            End If

            If isReplacementTextValid Then
                If newToken.IsBracketed Then
                    ' a reference location should always be tried to be unescaped, whether it was escaped before rename 
                    ' or the replacement itself is escaped.
                    newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation)
                Else
                    newToken = TryEscapeIdentifierToken(newToken)
                End If
            End If

            Return newToken
        End Function

        Private Function RenameInStringLiteral(oldToken As SyntaxToken, newToken As SyntaxToken, subSpanToReplacementString As ImmutableSortedDictionary(Of TextSpan, (String, String)), createNewStringLiteral As Func(Of SyntaxTriviaList, String, String, SyntaxTriviaList, SyntaxToken)) As SyntaxToken
            Dim originalString = newToken.ToString()
            Dim replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, subSpanToReplacementString)
            If replacedString <> originalString Then
                Dim oldSPan = oldToken.Span
                newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia)
                AddModifiedSpan(oldSPan, newToken.Span)
                Return oldToken.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSPan}))
            End If

            Return newToken
        End Function

        Private Function RenameInStringLiteral(oldToken As SyntaxToken, newToken As SyntaxToken, subSpansToReplace As ImmutableSortedSet(Of TextSpan), createNewStringLiteral As Func(Of SyntaxTriviaList, String, String, SyntaxTriviaList, SyntaxToken), renameSymbolContext As RenameSymbolContext) As SyntaxToken
            Dim originalString = newToken.ToString()
            Dim replacedString As String = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, renameSymbolContext.OriginalText, renameSymbolContext.ReplacementText, subSpansToReplace)
            If replacedString <> originalString Then
                Dim oldSpan = oldToken.Span
                newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia)
                AddModifiedSpan(oldSpan, newToken.Span)
                Return newToken.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
            End If

            Return newToken
        End Function

        Private Function RenameInCommentTrivia(trivia As SyntaxTrivia,
                                               subSpanToReplacementString As ImmutableSortedDictionary(Of TextSpan, (String, String))) As SyntaxTrivia
            Dim originalString = trivia.ToString()
            Dim replacedString As String = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, subSpanToReplacementString)
            If replacedString <> originalString Then
                Dim oldSpan = trivia.Span
                Dim newTrivia = SyntaxFactory.CommentTrivia(replacedString)
                AddModifiedSpan(oldSpan, newTrivia.Span)
                Return trivia.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newTrivia, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = oldSpan}))
            End If

            Return trivia
        End Function

        Private Function RenameTokenInStringOrComment(token As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
            Dim textSpanSymbolContexts As HashSet(Of TextSpanRenameContext) = Nothing
            If _isProcessingComplexifiedSpans OrElse Not _stringAndCommentRenameContexts.TryGetValue(token.Span, textSpanSymbolContexts) OrElse textSpanSymbolContexts.Count = 0 Then
                Return newToken
            End If

            Dim subSpanToReplacementText = RenameUtilities.CreateSubSpanToReplacementTextDictionary(textSpanSymbolContexts)

            Dim kind = newToken.Kind()
            If kind = SyntaxKind.StringLiteralToken Then
                newToken = RenameInStringLiteral(token, newToken, subSpanToReplacementText, AddressOf SyntaxFactory.StringLiteralToken)
            ElseIf kind = SyntaxKind.InterpolatedStringTextToken Then
                newToken = RenameInStringLiteral(token, newToken, subSpanToReplacementText, AddressOf SyntaxFactory.InterpolatedStringTextToken)
            ElseIf kind = SyntaxKind.XmlTextLiteralToken Then
                newToken = RenameInStringLiteral(token, newToken, subSpanToReplacementText, AddressOf SyntaxFactory.XmlTextLiteralToken)
            ElseIf kind = SyntaxKind.XmlNameToken Then
                Dim matchingContext = textSpanSymbolContexts.OrderByDescending(Function(c) c.Priority).FirstOrDefault(Function(c) CaseInsensitiveComparison.Equals(c.SymbolContext.OriginalText, newToken.ValueText))
                If matchingContext IsNot Nothing Then
                    Dim replacementText = matchingContext.SymbolContext.ReplacementText
                    Dim newIdentifierToken = SyntaxFactory.XmlNameToken(newToken.LeadingTrivia, replacementText, SyntaxFacts.GetKeywordKind(replacementText), newToken.TrailingTrivia)
                    newToken = token.CopyAnnotationsTo(Me._renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, New RenameTokenSimplificationAnnotation() With {.OriginalTextSpan = token.Span}))
                    AddModifiedSpan(token.Span, newToken.Span)
                End If
            End If

            Return newToken
        End Function
    End Class
End Namespace
