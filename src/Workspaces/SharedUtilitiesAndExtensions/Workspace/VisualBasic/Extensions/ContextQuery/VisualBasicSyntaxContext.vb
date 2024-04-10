' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    ''' <summary>
    ''' Helper structure to store some context about a position for keyword completion
    ''' </summary>
    Friend NotInheritable Class VisualBasicSyntaxContext
        Inherits SyntaxContext

        ''' <summary>
        ''' True if position is after a colon, or an
        ''' EOL that was not preceded by an explicit line continuation
        ''' </summary>
        Public ReadOnly FollowsEndOfStatement As Boolean

        ''' <summary>
        ''' True if position is definitely the beginning of a new statement (after a colon
        ''' or two line breaks).
        ''' 
        ''' Dim q = From a In args
        ''' $1
        ''' $2
        ''' 
        ''' $1 may continue the previous statement, but $2 definitely starts a 
        ''' new statement since there are two line breaks before it.
        ''' </summary>
        Public ReadOnly MustBeginNewStatement As Boolean

        Public ReadOnly IsCustomEventContext As Boolean
        Public ReadOnly IsInLambda As Boolean
        Public ReadOnly IsInterfaceMemberDeclarationKeywordContext As Boolean
        Public ReadOnly IsMultiLineStatementContext As Boolean
        Public ReadOnly IsPreprocessorEndDirectiveKeywordContext As Boolean
        Public ReadOnly IsPreprocessorStartContext As Boolean
        Public ReadOnly IsQueryOperatorContext As Boolean
        Public ReadOnly IsTypeDeclarationKeywordContext As Boolean
        Public ReadOnly IsTypeMemberDeclarationKeywordContext As Boolean
        Public ReadOnly IsWithinPreprocessorContext As Boolean

        Public ReadOnly ModifierCollectionFacts As ModifierCollectionFacts

        Public ReadOnly EnclosingNamedType As INamedTypeSymbol

        Private Sub New(
            document As Document,
            semanticModel As SemanticModel,
            position As Integer,
            leftToken As SyntaxToken,
            targetToken As SyntaxToken,
            isAttributeNameContext As Boolean,
            isAwaitKeywordContext As Boolean,
            isCustomEventContext As Boolean,
            isEnumBaseListContext As Boolean,
            isEnumTypeMemberAccessContext As Boolean,
            isAnyExpressionContext As Boolean,
            isGenericConstraintContext As Boolean,
            isGlobalStatementContext As Boolean,
            isOnArgumentListBracketOrComma As Boolean,
            isInImportsDirective As Boolean,
            isInLambda As Boolean,
            isInQuery As Boolean,
            isTaskLikeTypeContext As Boolean,
            isNameOfContext As Boolean,
            isNamespaceContext As Boolean,
            isNamespaceDeclarationNameContext As Boolean,
            isPossibleTupleContext As Boolean,
            isPreProcessorDirectiveContext As Boolean,
            isPreProcessorExpressionContext As Boolean,
            isRightAfterUsingOrImportDirective As Boolean,
            isRightOfNameSeparator As Boolean,
            isRightSideOfNumericType As Boolean,
            isStatementContext As Boolean,
            isTypeContext As Boolean,
            isWithinAsyncMethod As Boolean,
            cancellationToken As CancellationToken
        )
            MyBase.New(
                document,
                semanticModel,
                position,
                leftToken,
                targetToken,
                isAnyExpressionContext:=isAnyExpressionContext,
                isAtEndOfPattern:=False,
                isAtStartOfPattern:=False,
                isAttributeNameContext:=isAttributeNameContext,
                isAwaitKeywordContext:=isAwaitKeywordContext,
                isEnumBaseListContext:=isEnumBaseListContext,
                isEnumTypeMemberAccessContext:=isEnumTypeMemberAccessContext,
                isGenericConstraintContext:=isGenericConstraintContext,
                isGlobalStatementContext:=isGlobalStatementContext,
                isInImportsDirective:=isInImportsDirective,
                isInQuery:=isInQuery,
                isTaskLikeTypeContext:=isTaskLikeTypeContext,
                isNameOfContext:=isNameOfContext,
                isNamespaceContext,
                isNamespaceDeclarationNameContext,
                isOnArgumentListBracketOrComma:=isOnArgumentListBracketOrComma,
                isPossibleTupleContext:=isPossibleTupleContext,
                isPreProcessorDirectiveContext:=isPreProcessorDirectiveContext,
                isPreProcessorExpressionContext:=isPreProcessorExpressionContext,
                isRightAfterUsingOrImportDirective:=isRightAfterUsingOrImportDirective,
                isRightOfNameSeparator:=isRightOfNameSeparator,
                isRightSideOfNumericType:=isRightSideOfNumericType,
                isStatementContext:=isStatementContext,
                isTypeContext:=isTypeContext,
                isWithinAsyncMethod:=isWithinAsyncMethod,
                cancellationToken:=cancellationToken)

            Dim syntaxTree = semanticModel.SyntaxTree

            Me.FollowsEndOfStatement = targetToken.FollowsEndOfStatement(position)
            Me.MustBeginNewStatement = targetToken.MustBeginNewStatement(position)

            Me.IsMultiLineStatementContext = syntaxTree.IsMultiLineStatementStartContext(position, targetToken, cancellationToken)

            Me.IsTypeDeclarationKeywordContext = syntaxTree.IsTypeDeclarationKeywordContext(position, targetToken, cancellationToken)
            Me.IsTypeMemberDeclarationKeywordContext = syntaxTree.IsTypeMemberDeclarationKeywordContext(position, targetToken, cancellationToken)
            Me.IsInterfaceMemberDeclarationKeywordContext = syntaxTree.IsInterfaceMemberDeclarationKeywordContext(position, targetToken, cancellationToken)

            Me.ModifierCollectionFacts = New ModifierCollectionFacts(syntaxTree, position, targetToken, cancellationToken)
            Me.IsInLambda = isInLambda
            Me.IsPreprocessorStartContext = ComputeIsPreprocessorStartContext(position, targetToken)
            Me.IsWithinPreprocessorContext = ComputeIsWithinPreprocessorContext(position, targetToken)
            Me.IsQueryOperatorContext = syntaxTree.IsFollowingCompleteExpression(Of QueryExpressionSyntax)(position, targetToken, Function(query) query, cancellationToken)

            Me.EnclosingNamedType = ComputeEnclosingNamedType(cancellationToken)
            Me.IsCustomEventContext = isCustomEventContext

            Me.IsPreprocessorEndDirectiveKeywordContext = targetToken.FollowsBadEndDirective()
        End Sub

        Private Shared Function ComputeIsTaskLikeTypeContext(targetToken As SyntaxToken) As Boolean
            ' If we're after the 'as' in an async method declaration, then filter down to task-like types only.
            If targetToken.Kind() = SyntaxKind.AsKeyword Then
                Dim asClause = TryCast(targetToken.Parent, AsClauseSyntax)
                Dim methodStatement = TryCast(asClause?.Parent, MethodBaseSyntax)
                If methodStatement IsNot Nothing Then
                    Return methodStatement.Modifiers.Any(SyntaxKind.AsyncKeyword)
                End If
            End If

            Return False
        End Function

        Private Shared Shadows Function ComputeIsWithinAsyncMethod(targetToken As SyntaxToken) As Boolean
            Dim enclosingMethod = targetToken.GetAncestor(Of MethodBlockBaseSyntax)()
            Return enclosingMethod IsNot Nothing AndAlso enclosingMethod.BlockStatement.Modifiers.Any(SyntaxKind.AsyncKeyword)
        End Function

        Public Shared Function CreateContext(document As Document, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As VisualBasicSyntaxContext
            Dim syntaxTree = semanticModel.SyntaxTree
            Dim leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)

            Dim isAnyExpressionContext = syntaxTree.IsExpressionContext(position, targetToken, cancellationToken, semanticModel)
            Dim isInQuery = leftToken.GetAncestor(Of QueryExpressionSyntax)() IsNot Nothing
            Dim isStatementContext = syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)

            Return New VisualBasicSyntaxContext(
                document,
                semanticModel,
                position,
                leftToken,
                targetToken,
                isAnyExpressionContext:=isAnyExpressionContext,
                isAttributeNameContext:=syntaxTree.IsAttributeNameContext(position, targetToken, cancellationToken),
                isAwaitKeywordContext:=ComputeIsAwaitKeywordContext(targetToken, isAnyExpressionContext, isInQuery, isStatementContext),
                isCustomEventContext:=targetToken.GetAncestor(Of EventBlockSyntax)() IsNot Nothing,
                isEnumBaseListContext:=ComputeIsEnumBaseListContext(targetToken),
                isEnumTypeMemberAccessContext:=syntaxTree.IsEnumTypeMemberAccessContext(position, targetToken, semanticModel, cancellationToken),
                isGenericConstraintContext:=targetToken.Parent.IsKind(SyntaxKind.TypeParameterSingleConstraintClause, SyntaxKind.TypeParameterMultipleConstraintClause),
                isGlobalStatementContext:=syntaxTree.IsGlobalStatementContext(position, cancellationToken),
                isInImportsDirective:=leftToken.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing,
                isInLambda:=leftToken.GetAncestor(Of LambdaExpressionSyntax)() IsNot Nothing,
                isInQuery:=isInQuery,
                isTaskLikeTypeContext:=ComputeIsTaskLikeTypeContext(targetToken),
                isNameOfContext:=syntaxTree.IsNameOfContext(position, cancellationToken),
                isNamespaceContext:=syntaxTree.IsNamespaceContext(position, targetToken, cancellationToken, semanticModel),
                isNamespaceDeclarationNameContext:=syntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken),
                isOnArgumentListBracketOrComma:=targetToken.Parent.IsKind(SyntaxKind.ArgumentList),
                isPossibleTupleContext:=syntaxTree.IsPossibleTupleContext(targetToken, position),
                isPreProcessorDirectiveContext:=syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken),
                isPreProcessorExpressionContext:=syntaxTree.IsInPreprocessorExpressionContext(position, cancellationToken),
                isRightAfterUsingOrImportDirective:=ComputeIsRightAfterUsingOrImportDirective(targetToken),
                isRightOfNameSeparator:=syntaxTree.IsRightOfDot(position, cancellationToken),
                isRightSideOfNumericType:=False,
                isStatementContext:=isStatementContext,
                isTypeContext:=syntaxTree.IsTypeContext(position, targetToken, cancellationToken, semanticModel),
                isWithinAsyncMethod:=ComputeIsWithinAsyncMethod(targetToken),
                cancellationToken:=cancellationToken)
        End Function

        Private Shared Function ComputeIsAwaitKeywordContext(
                targetToken As SyntaxToken,
                isAnyExpressionContext As Boolean,
                isInQuery As Boolean,
                isSingleLineStatementContext As Boolean) As Boolean
            If isAnyExpressionContext OrElse isSingleLineStatementContext Then
                If isInQuery Then
                    ' There are some places where Await is allowed:
                    ' BC36929: 'Await' may only be used in a query expression within the first collection expression of the initial 'From' clause or within the collection expression of a 'Join' clause.
                    If targetToken.Kind = SyntaxKind.InKeyword Then
                        Dim collectionRange = TryCast(targetToken.Parent, CollectionRangeVariableSyntax)
                        If collectionRange IsNot Nothing Then
                            If TypeOf collectionRange.Parent Is FromClauseSyntax AndAlso TypeOf collectionRange.Parent.Parent Is QueryExpressionSyntax Then
                                Dim fromClause = DirectCast(collectionRange.Parent, FromClauseSyntax)
                                Dim queryExpression = DirectCast(collectionRange.Parent.Parent, QueryExpressionSyntax)
                                ' Await is only allowed for the first collection in a from clause. There are two forms to consider here:
                                ' 1. From x In xs From y In ys
                                ' 2. From x In xs, y In ys
                                ' 1. and 2. can be combined, but in any combination, Await is only allowed on the very first collection
                                If fromClause.Variables.FirstOrDefault() Is collectionRange AndAlso queryExpression.Clauses.FirstOrDefault() Is collectionRange.Parent Then
                                    Return True
                                End If
                            ElseIf TypeOf collectionRange.Parent Is SimpleJoinClauseSyntax OrElse TypeOf collectionRange.Parent Is GroupJoinClauseSyntax Then
                                Return True
                            End If
                        End If
                    End If

                    Return False
                End If
                For Each node In targetToken.GetAncestors(Of SyntaxNode)()
                    If node.IsKind(SyntaxKind.SingleLineSubLambdaExpression, SyntaxKind.SingleLineFunctionLambdaExpression,
                                   SyntaxKind.MultiLineSubLambdaExpression, SyntaxKind.MultiLineFunctionLambdaExpression) Then
                        Return True
                    End If

                    If node.IsKind(SyntaxKind.FinallyBlock, SyntaxKind.SyncLockBlock, SyntaxKind.CatchBlock) Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Private Function ComputeEnclosingNamedType(cancellationToken As CancellationToken) As INamedTypeSymbol
            ' It's possible the caller is asking about a speculative semantic model, and may have moved before the
            ' bounds of that model (for example, while looking at the nearby tokens around an edit).  If so, ensure we
            ' walk outwards to the correct model to actually ask this question of.
            Dim position = TargetToken.SpanStart
            Dim model = Me.SemanticModel
            If model.IsSpeculativeSemanticModel AndAlso position < model.OriginalPositionForSpeculation Then
                model = model.GetOriginalSemanticModel()
            End If

            Dim enclosingSymbol = model.GetEnclosingSymbol(position, cancellationToken)
            Return If(TryCast(enclosingSymbol, INamedTypeSymbol), enclosingSymbol.ContainingType)
        End Function

        Private Shared Function ComputeIsWithinPreprocessorContext(position As Integer, targetToken As SyntaxToken) As Boolean
            ' If we're touching it, then we can just look past it
            If targetToken.IsKind(SyntaxKind.HashToken) AndAlso targetToken.Span.End = position Then
                targetToken = targetToken.GetPreviousToken()
            End If

            Return targetToken.Kind = SyntaxKind.None OrElse
                targetToken.Kind = SyntaxKind.EndOfFileToken OrElse
                (targetToken.HasNonContinuableEndOfLineBeforePosition(position) AndAlso Not targetToken.FollowsBadEndDirective())
        End Function

        Private Shared Function ComputeIsPreprocessorStartContext(position As Integer, targetToken As SyntaxToken) As Boolean
            ' The triggering hash token must be part of a directive (not trivia within it)
            If targetToken.Kind = SyntaxKind.HashToken Then
                Return TypeOf targetToken.Parent Is DirectiveTriviaSyntax
            End If

            Return targetToken.Kind = SyntaxKind.None OrElse
                targetToken.Kind = SyntaxKind.EndOfFileToken OrElse
                (targetToken.HasNonContinuableEndOfLineBeforePosition(position) AndAlso Not targetToken.FollowsBadEndDirective())
        End Function

        Private Shared Function ComputeIsEnumBaseListContext(targetToken As SyntaxToken) As Boolean
            Dim enumDeclaration = targetToken.GetAncestor(Of EnumStatementSyntax)()
            Return enumDeclaration IsNot Nothing AndAlso
               enumDeclaration.UnderlyingType IsNot Nothing AndAlso
               targetToken = enumDeclaration.UnderlyingType.AsKeyword
        End Function

        Private Shared Function ComputeIsRightAfterUsingOrImportDirective(targetToken As SyntaxToken) As Boolean
            Dim importStatement = targetToken.GetAncestor(Function(n) n.IsKind(SyntaxKind.ImportsStatement))
            Dim lastToken = importStatement?.GetLastToken()
            Return lastToken.HasValue AndAlso lastToken.Value = targetToken
        End Function

        Public Function IsFollowingParameterListOrAsClauseOfMethodDeclaration() As Boolean
            If TargetToken.FollowsEndOfStatement(Position) Then
                Return False
            End If

            Dim methodDeclaration = TargetToken.GetAncestor(Of MethodStatementSyntax)()

            If methodDeclaration Is Nothing Then
                Return False
            End If

            ' We will trigger if either (a) we are after the ) of the parameter list, or (b) we are
            ' after the method name itself if the user is omitting the parenthesis, or (c) we are
            ' after the return type of the AsClause.
            Return (TargetToken.IsKind(SyntaxKind.CloseParenToken) AndAlso
                    methodDeclaration.ParameterList IsNot Nothing AndAlso
                    TargetToken = methodDeclaration.ParameterList.CloseParenToken) _
                   OrElse
                   (methodDeclaration.AsClause IsNot Nothing AndAlso
                    TargetToken = methodDeclaration.AsClause.GetLastToken(includeZeroWidth:=True)) _
                   OrElse
                   (TargetToken.IsKind(SyntaxKind.IdentifierToken) AndAlso
                    methodDeclaration.ParameterList Is Nothing AndAlso
                    TargetToken = methodDeclaration.Identifier)
        End Function

        Public Function IsFollowingCompleteEventDeclaration() As Boolean
            Dim eventDeclaration = TargetToken.GetAncestor(Of EventStatementSyntax)()
            If eventDeclaration Is Nothing Then
                Return False
            End If

            If eventDeclaration.AsClause IsNot Nothing Then
                Return TargetToken = eventDeclaration.AsClause.GetLastToken(includeZeroWidth:=True)
            End If

            If eventDeclaration.ParameterList IsNot Nothing AndAlso
                Not eventDeclaration.ParameterList.CloseParenToken.IsMissing AndAlso
                TargetToken = eventDeclaration.ParameterList.CloseParenToken Then
                Return True
            End If

            Return TargetToken = eventDeclaration.Identifier
        End Function

        Public Function IsFollowingCompletePropertyDeclaration(cancellationToken As CancellationToken) As Boolean
            Dim propertyDeclaration = TargetToken.GetAncestor(Of PropertyStatementSyntax)()
            If propertyDeclaration Is Nothing Then
                Return False
            End If

            If propertyDeclaration.Initializer IsNot Nothing Then
                Return SyntaxTree.IsFollowingCompleteExpression(
                    Position, TargetToken, Function(p As PropertyStatementSyntax) p.Initializer.Value, cancellationToken, allowImplicitLineContinuation:=False)
            End If

            If propertyDeclaration.AsClause IsNot Nothing Then
                Return TargetToken = propertyDeclaration.AsClause.GetLastToken(includeZeroWidth:=True)
            End If

            If propertyDeclaration.ParameterList IsNot Nothing AndAlso
                Not propertyDeclaration.ParameterList.CloseParenToken.IsMissing AndAlso
                TargetToken = propertyDeclaration.ParameterList.CloseParenToken Then
                Return True
            End If

            Return TargetToken = propertyDeclaration.Identifier
        End Function

        Public Function IsAdditionalJoinOperatorContext(cancellationToken As CancellationToken) As Boolean
            'This specifies if we're in a position where an additional "Join" operator may be present after a first Join
            'operator.
            Return SyntaxTree.IsFollowingCompleteExpression(Of JoinClauseSyntax)(
                Position, TargetToken, Function(joinOperator) joinOperator.JoinedVariables.LastCollectionExpression(), cancellationToken)
        End Function
    End Class
End Namespace

