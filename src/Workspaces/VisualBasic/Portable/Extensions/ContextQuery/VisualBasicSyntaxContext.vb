' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    ''' <summary>
    ''' Helper structure to store some context about a position for keyword completion
    ''' </summary>
    Friend NotInheritable Class VisualBasicSyntaxContext
        Inherits AbstractSyntaxContext

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

        Public ReadOnly IsSingleLineStatementContext As Boolean
        Public ReadOnly IsMultiLineStatementContext As Boolean

        Public ReadOnly IsGlobalStatementContext As Boolean

        Public ReadOnly IsTypeDeclarationKeywordContext As Boolean
        Public ReadOnly IsTypeMemberDeclarationKeywordContext As Boolean
        Public ReadOnly IsInterfaceMemberDeclarationKeywordContext As Boolean

        Public ReadOnly ModifierCollectionFacts As ModifierCollectionFacts
        Public ReadOnly IsPreprocessorStartContext As Boolean
        Public ReadOnly TouchingToken As SyntaxToken
        Public ReadOnly IsInLambda As Boolean
        Public ReadOnly IsQueryOperatorContext As Boolean
        Public ReadOnly EnclosingNamedType As CancellableLazy(Of INamedTypeSymbol)
        Public ReadOnly IsCustomEventContext As Boolean
        Public ReadOnly WithinAsyncMethod As Boolean

        Public ReadOnly IsPreprocessorEndDirectiveKeywordContext As Boolean
        Public ReadOnly IsWithinPreprocessorContext As Boolean

        Private Sub New(
            workspace As Workspace,
            semanticModel As SemanticModel,
            position As Integer,
            leftToken As SyntaxToken,
            targetToken As SyntaxToken,
            touchingToken As SyntaxToken,
            isTypeContext As Boolean,
            isNamespaceContext As Boolean,
            isPreProcessorDirectiveContext As Boolean,
            isRightOfNameSeparator As Boolean,
            isSingleLineStatementContext As Boolean,
            isExpressionContext As Boolean,
            isAttributeNameContext As Boolean,
            isEnumTypeMemberAccessContext As Boolean,
            isNameOfContext As Boolean,
            isInLambda As Boolean,
            isInQuery As Boolean,
            isInImportsDirective As Boolean,
            isCustomEventContext As Boolean,
            cancellationToken As CancellationToken
        )

            MyBase.New(
                workspace,
                semanticModel,
                position,
                leftToken,
                targetToken,
                isTypeContext,
                isNamespaceContext,
                isPreProcessorDirectiveContext,
                isRightOfNameSeparator,
                isStatementContext:=isSingleLineStatementContext,
                isAnyExpressionContext:=isExpressionContext,
                isAttributeNameContext:=isAttributeNameContext,
                isEnumTypeMemberAccessContext:=isEnumTypeMemberAccessContext,
                isNameOfContext:=isNameOfContext,
                isInQuery:=isInQuery,
                isInImportsDirective:=isInImportsDirective)

            Dim syntaxTree = semanticModel.SyntaxTree

            Me.FollowsEndOfStatement = targetToken.FollowsEndOfStatement(position)
            Me.MustBeginNewStatement = targetToken.MustBeginNewStatement(position)

            Me.IsSingleLineStatementContext = isSingleLineStatementContext
            Me.IsMultiLineStatementContext = syntaxTree.IsMultiLineStatementStartContext(position, targetToken, cancellationToken)

            Me.IsGlobalStatementContext = syntaxTree.IsGlobalStatementContext(position, cancellationToken)

            Me.IsTypeDeclarationKeywordContext = syntaxTree.IsTypeDeclarationKeywordContext(position, targetToken, cancellationToken)
            Me.IsTypeMemberDeclarationKeywordContext = syntaxTree.IsTypeMemberDeclarationKeywordContext(position, targetToken, cancellationToken)
            Me.IsInterfaceMemberDeclarationKeywordContext = syntaxTree.IsInterfaceMemberDeclarationKeywordContext(position, targetToken, cancellationToken)

            Me.ModifierCollectionFacts = New ModifierCollectionFacts(syntaxTree, position, targetToken, cancellationToken)
            Me.TouchingToken = touchingToken
            Me.IsInLambda = isInLambda
            Me.IsPreprocessorStartContext = ComputeIsPreprocessorStartContext(position, targetToken)
            Me.IsWithinPreprocessorContext = ComputeIsWithinPreprocessorContext(position, targetToken)
            Me.IsQueryOperatorContext = syntaxTree.IsFollowingCompleteExpression(Of QueryExpressionSyntax)(position, targetToken, Function(query) query, cancellationToken)

            Me.EnclosingNamedType = CancellableLazy.Create(AddressOf ComputeEnclosingNamedType)
            Me.IsCustomEventContext = isCustomEventContext
            Me.WithinAsyncMethod = IsWithinAsyncMethod(targetToken, cancellationToken)

            Me.IsPreprocessorEndDirectiveKeywordContext = targetToken.FollowsBadEndDirective(position)
        End Sub

        Private Function IsWithinAsyncMethod(targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Dim enclosingMethod = targetToken.GetAncestor(Of MethodBlockBaseSyntax)()
            Return enclosingMethod IsNot Nothing AndAlso enclosingMethod.BlockStatement.Modifiers.Any(SyntaxKind.AsyncKeyword)
        End Function

        Public Shared Async Function CreateContextAsync(workspace As Workspace, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Tasks.Task(Of VisualBasicSyntaxContext)
            Dim syntaxTree = semanticModel.SyntaxTree
            Dim leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)
            Dim touchingToken = Await syntaxTree.GetTouchingTokenAsync(position, cancellationToken).ConfigureAwait(False)

            Return New VisualBasicSyntaxContext(
                workspace,
                semanticModel,
                position,
                leftToken,
                targetToken,
                touchingToken,
                isTypeContext:=syntaxTree.IsTypeContext(position, targetToken, cancellationToken, semanticModel),
                isNamespaceContext:=syntaxTree.IsNamespaceContext(position, targetToken, cancellationToken, semanticModel),
                isPreProcessorDirectiveContext:=syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken),
                isRightOfNameSeparator:=syntaxTree.IsRightOfDot(position, cancellationToken),
                isSingleLineStatementContext:=syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken),
                isExpressionContext:=syntaxTree.IsExpressionContext(position, targetToken, cancellationToken, semanticModel),
                isAttributeNameContext:=syntaxTree.IsAttributeNameContext(position, targetToken, cancellationToken),
                isEnumTypeMemberAccessContext:=syntaxTree.IsEnumTypeMemberAccessContext(position, targetToken, semanticModel, cancellationToken),
                isNameOfContext:=syntaxTree.IsNameOfContext(position, cancellationToken),
                isInLambda:=leftToken.GetAncestor(Of LambdaExpressionSyntax)() IsNot Nothing,
                isInQuery:=leftToken.GetAncestor(Of QueryExpressionSyntax)() IsNot Nothing,
                isInImportsDirective:=leftToken.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing,
                isCustomEventContext:=targetToken.GetAncestor(Of EventBlockSyntax)() IsNot Nothing,
                cancellationToken:=cancellationToken)
        End Function

        Public Shared Function CreateContextAsync_Test(semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As Tasks.Task(Of VisualBasicSyntaxContext)
            Return CreateContextAsync(Nothing, semanticModel, position, cancellationToken)
        End Function

        Private Function ComputeEnclosingNamedType(cancellationToken As CancellationToken) As INamedTypeSymbol
            Dim enclosingSymbol = Me.SemanticModel.GetEnclosingSymbol(Me.TargetToken.SpanStart, cancellationToken)
            Dim container = TryCast(enclosingSymbol, INamedTypeSymbol)
            If container Is Nothing Then
                container = enclosingSymbol.ContainingType
            End If

            Return container
        End Function

        Private Shared Function ComputeIsWithinPreprocessorContext(position As Integer, targetToken As SyntaxToken) As Boolean
            ' If we're touching it, then we can just look past it
            If targetToken.IsKind(SyntaxKind.HashToken) AndAlso targetToken.Span.End = position Then
                targetToken = targetToken.GetPreviousToken()
            End If

            Return targetToken.Kind = SyntaxKind.None OrElse
                targetToken.Kind = SyntaxKind.EndOfFileToken OrElse
                (targetToken.HasNonContinuableEndOfLineBeforePosition(position) AndAlso Not targetToken.FollowsBadEndDirective(position))
        End Function

        Private Shared Function ComputeIsPreprocessorStartContext(position As Integer, targetToken As SyntaxToken) As Boolean
            ' The triggering hash token must be part of a directive (not trivia within it)
            If targetToken.Kind = SyntaxKind.HashToken Then
                Return TypeOf targetToken.Parent Is DirectiveTriviaSyntax
            End If

            Return targetToken.Kind = SyntaxKind.None OrElse
                targetToken.Kind = SyntaxKind.EndOfFileToken OrElse
                (targetToken.HasNonContinuableEndOfLineBeforePosition(position) AndAlso Not targetToken.FollowsBadEndDirective(position))
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

