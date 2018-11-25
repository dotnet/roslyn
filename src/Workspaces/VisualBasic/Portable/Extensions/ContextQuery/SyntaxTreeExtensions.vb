' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    Friend Module SyntaxTreeExtensions

        <Extension()>
        Friend Function GetTargetToken(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(
                position, cancellationToken,
                includeDirectives:=syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken),
                includeDocumentationComments:=True)

            Do While token.Kind <> SyntaxKind.None
                ' If we have a non-word token to our left, we should always stop there
                If Not token.IsWord() AndAlso token.Span.End <= position Then
                    Exit Do
                End If

                ' If this token is to our left, return it
                If Not token.IsKind(SyntaxKind.EmptyToken) AndAlso token.Span.End < position Then
                    Exit Do
                End If

                token = token.GetPreviousToken()
            Loop

            Return token
        End Function

        <Extension()>
        Public Function IsPreProcessorKeywordContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Return IsPreProcessorKeywordContext(
                syntaxTree, position,
                syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives:=True),
                cancellationToken)
        End Function

        <Extension()>
        Public Function IsPreProcessorKeywordContext(syntaxTree As SyntaxTree, position As Integer, preProcessorTokenOnLeftOfPosition As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            ' cases:
            '  #|
            '  #d|
            '  # |
            '  # d|

            ' note comments are Not allowed between the # And item.
            Dim token = preProcessorTokenOnLeftOfPosition
            token = token.GetPreviousTokenIfTouchingWord(position)

            Return token.HasAncestor(Of DirectiveTriviaSyntax)
        End Function

        <Extension()>
        Public Function IsNamespaceContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken, Optional semanticModelOpt As SemanticModel = Nothing) As Boolean
            Return syntaxTree.IsTypeContext(position, token, cancellationToken, semanticModelOpt)
        End Function

        <Extension()>
        Public Function IsNamespaceDeclarationNameContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            If syntaxTree.IsScript() OrElse syntaxTree.IsInNonUserCode(position, cancellationToken) Then
                Return False
            End If

            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken) _
                                  .GetPreviousTokenIfTouchingWord(position)

            Dim statement = token.GetAncestor(Of NamespaceStatementSyntax)()

            Return statement IsNot Nothing AndAlso (statement.Name.Span.IntersectsWith(position) OrElse statement.NamespaceKeyword = token)
        End Function

        <Extension()>
        Public Function IsPartialTypeDeclarationNameContext(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken, ByRef statementSyntax As TypeStatementSyntax) As Boolean
            If tree.IsInNonUserCode(position, cancellationToken) OrElse tree.IsInSkippedText(position, cancellationToken) Then
                Return False
            End If

            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken) _
                            .GetPreviousTokenIfTouchingWord(position)

            Select Case token.Kind()
                Case SyntaxKind.ClassKeyword,
                     SyntaxKind.StructureKeyword,
                     SyntaxKind.InterfaceKeyword,
                     SyntaxKind.ModuleKeyword

                    statementSyntax = token.GetAncestor(Of TypeStatementSyntax)()
                    Return statementSyntax IsNot Nothing AndAlso
                           statementSyntax.DeclarationKeyword = token AndAlso
                           statementSyntax.Modifiers.Any(SyntaxKind.PartialKeyword)
            End Select

            Return False
        End Function

        <Extension()>
        Public Function GetContainingTypeBlock(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As TypeBlockSyntax
            Dim token = syntaxTree.GetRoot(cancellationToken).FindToken(position)
            Return TryCast(token.GetInnermostDeclarationContext(), TypeBlockSyntax)
        End Function

        ''' <summary>
        ''' The specified position is where we can declare some .NET type, such as classes, structures, etc.
        ''' </summary>
        <Extension()>
        Friend Function IsTypeDeclarationContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return Not syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) AndAlso
                syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.NamespaceBlock, SyntaxKind.ModuleBlock, SyntaxKind.CompilationUnit)
        End Function

        <Extension()>
        Friend Function IsDeclarationContextWithinTypeBlocks(
            syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, allowAfterModifiersOrDim As Boolean, cancellationToken As CancellationToken, ParamArray allowedParentBlocks As SyntaxKind()) As Boolean

            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.Kind = SyntaxKind.None OrElse targetToken.Parent Is Nothing Then
                ' We're at the root, so we're acceptable if we allow us to be in the root
                Return allowedParentBlocks.Contains(SyntaxKind.CompilationUnit)
            End If

            If syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) Then
                Return False
            End If

            ' If we're within a method/event/property, then always no
            Dim method = targetToken.GetAncestor(Of MethodBlockBaseSyntax)()
            If method IsNot Nothing AndAlso
               (method.EndBlockStatement Is Nothing OrElse
                method.EndBlockStatement.IsMissing OrElse
                method.EndBlockStatement.BlockKeyword <> targetToken) Then

                Return False
            End If

            Dim [event] = targetToken.GetAncestor(Of EventBlockSyntax)()
            If [event] IsNot Nothing AndAlso
               ([event].EndEventStatement Is Nothing OrElse
                [event].EndEventStatement.IsMissing OrElse
                [event].EndEventStatement.BlockKeyword <> targetToken) Then

                Return False
            End If

            Dim afterDimOrModifiers = allowAfterModifiersOrDim AndAlso IsDimOrModifierOrAttributeList(targetToken)

            ' We either must be on a separate line, or else after Dim or modifiers
            If targetToken.FollowsEndOfStatement(position) OrElse afterDimOrModifiers Then
                Return targetToken.GetInnermostDeclarationContext().IsKind(allowedParentBlocks)
            End If

            Return False
        End Function

        Private Function IsDimOrModifierOrAttributeList(token As SyntaxToken) As Boolean
            If token.IsModifier Then
                Return True
            End If

            If token.Kind = SyntaxKind.DimKeyword Then
                Return True
            End If

            If token.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse token.HasMatchingText(SyntaxKind.IteratorKeyword) Then
                Return True
            End If

            ' eg. <Extension> |
            If token.Kind = SyntaxKind.GreaterThanToken AndAlso token.Parent.Kind = SyntaxKind.AttributeList Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' The specified position is where a keyword can go like "Sub", "Function", etc. in a classes, structures, and modules
        ''' </summary>
        <Extension()>
        Friend Function IsTypeMemberDeclarationKeywordContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return syntaxTree.IsDeclarationContextWithinTypeBlocks(
                position, token, True, cancellationToken, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock)
        End Function

        ''' <summary>
        ''' The specified position is where a keyword can go like "Sub", "Function" in an interface
        ''' </summary>
        <Extension()>
        Friend Function IsInterfaceMemberDeclarationKeywordContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.InterfaceBlock)
        End Function

        ''' <summary>
        ''' The specified position is where we can declare some .NET type, such as classes, structures, etc.
        ''' </summary>
        <Extension()>
        Friend Function IsTypeDeclarationKeywordContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return Not syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) AndAlso
                syntaxTree.IsDeclarationContextWithinTypeBlocks(
                    position, token, True, cancellationToken, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.NamespaceBlock, SyntaxKind.ModuleBlock, SyntaxKind.CompilationUnit)
        End Function

        <Extension>
        Friend Function IsFieldNameDeclarationContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            If targetToken.IsKind(SyntaxKind.ConstKeyword,
                                 SyntaxKind.DimKeyword,
                                 SyntaxKind.FriendKeyword,
                                 SyntaxKind.PrivateKeyword,
                                 SyntaxKind.ProtectedKeyword,
                                 SyntaxKind.ReadOnlyKeyword,
                                 SyntaxKind.PublicKeyword,
                                 SyntaxKind.ShadowsKeyword,
                                 SyntaxKind.SharedKeyword,
                                 SyntaxKind.WithEventsKeyword) Then

                Dim typeBlock = targetToken.GetAncestor(Of TypeBlockSyntax)()

                If typeBlock IsNot Nothing AndAlso
                       typeBlock.IsKind(SyntaxKind.ClassBlock,
                                             SyntaxKind.ModuleBlock,
                                             SyntaxKind.StructureBlock) Then

                    Dim modifierFacts = New ModifierCollectionFacts(syntaxTree, position, targetToken, cancellationToken)
                    Return modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field)
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsParameterNameDeclarationContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)

            If targetToken.Parent.IsKind(SyntaxKind.ParameterList) AndAlso
                targetToken.IsKind(SyntaxKind.OpenParenToken,
                                  SyntaxKind.CommaToken) Then

                Return True
            End If

            If targetToken.Parent.IsKind(SyntaxKind.Parameter) AndAlso
                targetToken.IsKind(SyntaxKind.ByValKeyword,
                                  SyntaxKind.ByRefKeyword,
                                  SyntaxKind.ParamArrayKeyword,
                                  SyntaxKind.OptionalKeyword) Then

                Return True
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsLabelContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            Dim gotoStatement = targetToken.GetAncestor(Of GoToStatementSyntax)()
            If gotoStatement IsNot Nothing Then
                If gotoStatement.GoToKeyword = targetToken Then
                    Return True
                End If

                If gotoStatement.Label.LabelToken = targetToken AndAlso targetToken.IntersectsWith(position) Then
                    Return True
                End If
            End If

            Dim onErrorGotoStatement = targetToken.GetAncestor(Of OnErrorGoToStatementSyntax)()
            If onErrorGotoStatement IsNot Nothing Then
                If onErrorGotoStatement.GoToKeyword = targetToken Then
                    Return True
                End If

                If onErrorGotoStatement.Label.LabelToken = targetToken AndAlso targetToken.IntersectsWith(position) Then
                    Return True
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsEnumMemberNameContext(syntaxTree As SyntaxTree, context As VisualBasicSyntaxContext) As Boolean
            Dim token = context.TargetToken

            ' Check to see if we're inside an enum block
            Dim enumBlock = token.GetAncestor(Of EnumBlockSyntax)()
            If enumBlock IsNot Nothing Then
                Return context.FollowsEndOfStatement OrElse token.IsChildToken(Of EnumMemberDeclarationSyntax)(Function(emds) emds.Identifier)
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsDelegateCreationContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))

            If targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            If targetToken.Parent.IsKind(SyntaxKind.ArgumentList) AndAlso
               TypeOf targetToken.Parent.Parent Is NewExpressionSyntax Then

                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(targetToken.Parent.Parent, NewExpressionSyntax).Type())
                Dim objectCreationType = TryCast(symbolInfo.Symbol, ITypeSymbol)
                If objectCreationType IsNot Nothing AndAlso
                   objectCreationType.TypeKind = TypeKind.Delegate Then

                    Return True
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsExpressionContext(
            syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken, Optional semanticModelOpt As SemanticModel = Nothing) As Boolean
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)
            Return IsExpressionContext(syntaxTree, position, targetToken, cancellationToken, semanticModelOpt)
        End Function

        <Extension()>
        Friend Function IsExpressionContext(
            syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken, Optional semanticModelOpt As SemanticModel = Nothing) As Boolean

            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))

            ' Tuple elements are in expression context if the tuple is in expression context
            PositionOutsideTupleIfApplicable(syntaxTree, position, targetToken, cancellationToken)

            If targetToken.FollowsEndOfStatement(position) OrElse targetToken.Kind = SyntaxKind.None Then
                Return False
            End If

            If semanticModelOpt IsNot Nothing Then
                If syntaxTree.IsDelegateCreationContext(position, targetToken, semanticModelOpt, cancellationToken) Then
                    Return False
                End If
            End If

            ' Easy ones first
            If targetToken.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(handlerStatement) handlerStatement.AddHandlerOrRemoveHandlerKeyword) OrElse
               targetToken.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(handlerStatement) handlerStatement.CommaToken) OrElse
               targetToken.IsChildToken(Of ArgumentListSyntax)(Function(argumentList) argumentList.OpenParenToken) OrElse
               targetToken.IsChildToken(Of AssignmentStatementSyntax)(Function(assignmentStatement) assignmentStatement.OperatorToken) OrElse
               targetToken.IsChildToken(Of AwaitExpressionSyntax)(Function(awaitExpression) awaitExpression.AwaitKeyword) OrElse
               targetToken.IsChildToken(Of BinaryExpressionSyntax)(Function(binaryExpression) binaryExpression.OperatorToken) OrElse
               targetToken.IsChildToken(Of BinaryConditionalExpressionSyntax)(Function(binaryExpression) binaryExpression.OpenParenToken) OrElse
               targetToken.IsChildToken(Of BinaryConditionalExpressionSyntax)(Function(binaryExpression) binaryExpression.CommaToken) OrElse
               targetToken.IsChildToken(Of CallStatementSyntax)(Function(callStatementSyntax) callStatementSyntax.CallKeyword) OrElse
               targetToken.IsChildToken(Of CatchFilterClauseSyntax)(Function(catchFilterClauseSyntax) catchFilterClauseSyntax.WhenKeyword) OrElse
               targetToken.IsChildToken(Of CaseStatementSyntax)(Function(caseStatement) caseStatement.CaseKeyword) OrElse
               targetToken.IsChildToken(Of ConditionalAccessExpressionSyntax)(Function(conditionalAccessExpressionSyntax) conditionalAccessExpressionSyntax.QuestionMarkToken) OrElse
               targetToken.IsChildSeparatorToken(Of CaseStatementSyntax, CaseClauseSyntax)(Function(caseStatement) caseStatement.Cases) OrElse
               targetToken.IsChildToken(Of RangeCaseClauseSyntax)(Function(rangeCaseClause) rangeCaseClause.ToKeyword) OrElse
               targetToken.IsChildToken(Of RelationalCaseClauseSyntax)(Function(relationalCaseClause) relationalCaseClause.OperatorToken) OrElse
               targetToken.IsChildToken(Of CastExpressionSyntax)(Function(castExpression) castExpression.OpenParenToken) OrElse
               targetToken.IsChildToken(Of CollectionInitializerSyntax)(Function(collectionInitializer) collectionInitializer.OpenBraceToken) OrElse
               targetToken.IsChildToken(Of CollectionRangeVariableSyntax)(Function(collectionRange) collectionRange.InKeyword) OrElse
               targetToken.IsChildToken(Of EraseStatementSyntax)(Function(eraseStatement) eraseStatement.EraseKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of EraseStatementSyntax, ExpressionSyntax)(Function(eraseStatement) eraseStatement.Expressions) OrElse
               targetToken.IsChildToken(Of ErrorStatementSyntax)(Function(errorStatement) errorStatement.ErrorKeyword) OrElse
               targetToken.IsChildToken(Of ForStatementSyntax)(Function(forStatement) forStatement.EqualsToken) OrElse
               targetToken.IsChildToken(Of ForStatementSyntax)(Function(forStatement) forStatement.ToKeyword) OrElse
               targetToken.IsChildToken(Of ForStepClauseSyntax)(Function(forStepClause) forStepClause.StepKeyword) OrElse
               targetToken.IsChildToken(Of ForEachStatementSyntax)(Function(forEachStatement) forEachStatement.InKeyword) OrElse
               targetToken.IsChildToken(Of FunctionAggregationSyntax)(Function(functionAggregation) functionAggregation.OpenParenToken) OrElse
               targetToken.IsChildToken(Of GetTypeExpressionSyntax)(Function(getTypeExpression) getTypeExpression.OpenParenToken) OrElse
               targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) OrElse
               targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.ByKeyword) OrElse
               targetToken.IsChildToken(Of IfStatementSyntax)(Function(ifStatement) ifStatement.IfKeyword) OrElse
               targetToken.IsChildToken(Of ElseIfStatementSyntax)(Function(elseIfStatement) elseIfStatement.ElseIfKeyword) OrElse
               targetToken.IsChildToken(Of InferredFieldInitializerSyntax)(Function(inferredField) inferredField.KeyKeyword) OrElse
               targetToken.IsChildToken(Of InterpolationSyntax)(Function(interpolation) interpolation.OpenBraceToken) OrElse
               targetToken.IsChildToken(Of EqualsValueSyntax)(Function(initializer) initializer.EqualsToken) OrElse
               targetToken.IsChildToken(Of JoinClauseSyntax)(Function(joinQuery) joinQuery.OnKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of JoinClauseSyntax, JoinConditionSyntax)(Function(joinQuery) joinQuery.JoinConditions) OrElse
               targetToken.IsChildToken(Of JoinConditionSyntax)(Function(joinCondition) joinCondition.EqualsKeyword) OrElse
               targetToken.IsChildToken(Of SimpleArgumentSyntax)(Function(argument) If(argument.IsNamed, argument.NameColonEquals.ColonEqualsToken, Nothing)) OrElse
               targetToken.IsChildToken(Of NamedFieldInitializerSyntax)(Function(namedField) namedField.EqualsToken) OrElse
               targetToken.IsChildToken(Of OrderByClauseSyntax)(Function(orderByClause) orderByClause.ByKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of OrderByClauseSyntax, OrderingSyntax)(Function(orderByClause) orderByClause.Orderings) OrElse
               targetToken.IsChildToken(Of ParenthesizedExpressionSyntax)(Function(parenthesizedExpression) parenthesizedExpression.OpenParenToken) OrElse
               targetToken.IsChildToken(Of PartitionClauseSyntax)(Function(partitionClause) partitionClause.SkipOrTakeKeyword) OrElse
               targetToken.IsChildToken(Of PartitionWhileClauseSyntax)(Function(partitionWhileClause) partitionWhileClause.WhileKeyword) OrElse
               targetToken.IsChildToken(Of PredefinedCastExpressionSyntax)(Function(buildInCast) buildInCast.OpenParenToken) OrElse
               targetToken.IsChildToken(Of RangeArgumentSyntax)(Function(rangeArgument) rangeArgument.ToKeyword) OrElse
               targetToken.IsChildToken(Of ReDimStatementSyntax)(Function(redimStatement) redimStatement.ReDimKeyword) OrElse
               targetToken.IsChildToken(Of ReDimStatementSyntax)(Function(redimStatement) redimStatement.PreserveKeyword) OrElse
               targetToken.IsChildToken(Of ReturnStatementSyntax)(Function(returnStatement) returnStatement.ReturnKeyword) OrElse
               targetToken.IsChildToken(Of SelectClauseSyntax)(Function(selectClause) selectClause.SelectKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of SelectClauseSyntax, ExpressionRangeVariableSyntax)(Function(selectClause) selectClause.Variables) OrElse
               targetToken.IsChildToken(Of SelectStatementSyntax)(Function(selectStatement) selectStatement.SelectKeyword) OrElse
               targetToken.IsChildToken(Of SelectStatementSyntax)(Function(selectStatement) selectStatement.CaseKeyword) OrElse
               targetToken.IsChildToken(Of SyncLockStatementSyntax)(Function(syncLockStatement) syncLockStatement.SyncLockKeyword) OrElse
               targetToken.IsChildToken(Of TernaryConditionalExpressionSyntax)(Function(ternaryConditional) ternaryConditional.OpenParenToken) OrElse
               targetToken.IsChildToken(Of TernaryConditionalExpressionSyntax)(Function(ternaryConditional) ternaryConditional.FirstCommaToken) OrElse
               targetToken.IsChildToken(Of TernaryConditionalExpressionSyntax)(Function(ternaryConditional) ternaryConditional.SecondCommaToken) OrElse
               targetToken.IsChildToken(Of ThrowStatementSyntax)(Function(throwStatement) throwStatement.ThrowKeyword) OrElse
               targetToken.IsChildToken(Of TypeOfExpressionSyntax)(Function(typeOfIsExpression) typeOfIsExpression.TypeOfKeyword) OrElse
               targetToken.IsChildToken(Of UnaryExpressionSyntax)(Function(unaryExpression) unaryExpression.OperatorToken) OrElse
               targetToken.IsChildToken(Of UsingStatementSyntax)(Function(usingStatementSyntax) usingStatementSyntax.UsingKeyword) OrElse
               targetToken.IsChildToken(Of VariableNameEqualsSyntax)(Function(variableNameEquals) variableNameEquals.EqualsToken) OrElse
               targetToken.IsChildToken(Of WhereClauseSyntax)(Function(whereClause) whereClause.WhereKeyword) OrElse
               targetToken.IsChildToken(Of WhileStatementSyntax)(Function(whileStatement) whileStatement.WhileKeyword) OrElse
               targetToken.IsChildToken(Of WhileOrUntilClauseSyntax)(Function(whileUntilClause) whileUntilClause.WhileOrUntilKeyword) OrElse
               targetToken.IsChildToken(Of WithStatementSyntax)(Function(withStatement) withStatement.WithKeyword) OrElse
               targetToken.IsChildToken(Of XmlEmbeddedExpressionSyntax)(Function(xmlEmbeddedExpression) xmlEmbeddedExpression.LessThanPercentEqualsToken) OrElse
               targetToken.IsChildToken(Of YieldStatementSyntax)(Function(yieldStatement) yieldStatement.YieldKeyword) Then
                Return True
            End If

            ' The close paren of the parameter list of a single-line lambda?
            If targetToken.Kind = SyntaxKind.CloseParenToken AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ParameterList) AndAlso
               TypeOf targetToken.Parent.Parent Is LambdaHeaderSyntax Then
                Return True
            End If

            ' A comma in a method call or collection initializer?
            If targetToken.Kind = SyntaxKind.CommaToken AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.CollectionInitializer, SyntaxKind.EraseStatement) Then

                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsAttributeNameContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            Debug.Assert(Not (targetToken.IntersectsWith(position) AndAlso IsWord(targetToken)))

            If targetToken.IsChildToken(Function(a As AttributeTargetSyntax) a.ColonToken) OrElse
               targetToken.IsChildToken(Function(a As AttributeListSyntax) a.LessThanToken) OrElse
               targetToken.IsChildSeparatorToken(Function(a As AttributeListSyntax) a.Attributes) Then
                Return True
            End If

            If targetToken.IsKind(SyntaxKind.DotToken) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.QualifiedName) AndAlso
               targetToken.Parent.Parent.IsKind(SyntaxKind.Attribute) Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsTypeContext(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken, Optional semanticModelOpt As SemanticModel = Nothing) As Boolean
            ' first do quick exit check
            If syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) OrElse
               syntaxTree.IsInInactiveRegion(position, cancellationToken) OrElse
               syntaxTree.IsEntirelyWithinComment(position, cancellationToken) OrElse
               syntaxTree.IsEntirelyWithinStringOrCharOrNumericLiteral(position, cancellationToken) Then

                Return False
            End If

            Debug.Assert(token = syntaxTree.GetTargetToken(position, cancellationToken))

            ' Tuple elements are in type context if the tuple is in type context
            PositionOutsideTupleIfApplicable(syntaxTree, position, token, cancellationToken)

            ' Types may start anywhere a full expression may be given
            If syntaxTree.IsExpressionContext(position, token, cancellationToken, semanticModelOpt) Then
                Return True
            End If

            ' Types may also start a statement
            If syntaxTree.IsSingleLineStatementContext(position, token, cancellationToken) Then
                Return True
            End If

            If syntaxTree.IsAttributeNameContext(position, token, cancellationToken) Then
                Return True
            End If

            ' Simple cases first
            If token.IsChildToken(Of ImportAliasClauseSyntax)(Function(importAliasClause) importAliasClause.EqualsToken) OrElse
               token.IsChildToken(Of ArrayCreationExpressionSyntax)(Function(arrayCreation) arrayCreation.NewKeyword) OrElse
               token.IsChildToken(Of AsNewClauseSyntax)(Function(asNewClause) asNewClause.NewExpression.NewKeyword) OrElse
               token.IsChildToken(Of InheritsStatementSyntax)(Function(node) node.InheritsKeyword) OrElse
               token.IsChildSeparatorToken(Of InheritsStatementSyntax, TypeSyntax)(Function(baseDeclaration) baseDeclaration.Types) OrElse
               token.IsChildToken(Of ImplementsStatementSyntax)(Function(node) node.ImplementsKeyword) OrElse
               token.IsChildSeparatorToken(Of ImplementsStatementSyntax, TypeSyntax)(Function(baseDeclaration) baseDeclaration.Types) OrElse
               token.IsChildToken(Of CastExpressionSyntax)(Function(castExpression) castExpression.CommaToken) OrElse
               token.IsChildToken(Of ImplementsClauseSyntax)(Function(implementsClause) implementsClause.ImplementsKeyword) OrElse
               token.IsChildSeparatorToken(Of ImplementsClauseSyntax, QualifiedNameSyntax)(Function(implementsClause) implementsClause.InterfaceMembers) OrElse
               token.IsChildToken(Of ImportsStatementSyntax)(Function(importsStatement) importsStatement.ImportsKeyword) OrElse
               token.IsChildSeparatorToken(Of ImportsStatementSyntax, ImportsClauseSyntax)(Function(importsStatement) importsStatement.ImportsClauses) OrElse
               token.IsChildToken(Of ObjectCreationExpressionSyntax)(Function(objectCreation) objectCreation.NewKeyword) OrElse
               token.IsChildToken(Of TypeArgumentListSyntax)(Function(typeArgumentList) typeArgumentList.OfKeyword) OrElse
               token.IsChildSeparatorToken(Of TypeArgumentListSyntax, TypeSyntax)(Function(typeArgumentList) typeArgumentList.Arguments) OrElse
               token.IsChildToken(Of TypeOfExpressionSyntax)(Function(typeOfIs) typeOfIs.OperatorToken) OrElse
               token.IsChildToken(Of TypeParameterSingleConstraintClauseSyntax)(Function(constraint) constraint.AsKeyword) OrElse
               token.IsChildToken(Of TypeParameterMultipleConstraintClauseSyntax)(Function(constraint) constraint.OpenBraceToken) OrElse
               token.IsChildSeparatorToken(Of TypeParameterMultipleConstraintClauseSyntax, ConstraintSyntax)(Function(constraint) constraint.Constraints) Then
                Return True
            End If

            Dim parent = token.Parent
            If parent Is Nothing Then
                Return False
            End If

            ' If we're in an Enum's underlying type, we never recommend...
            If parent.IsChildNode(Of EnumStatementSyntax)(Function(enumDeclaration) enumDeclaration.UnderlyingType) Then
                Return False
            End If

            ' ...otherwise any other SimpleAsClause is good
            Return token.IsChildToken(Of SimpleAsClauseSyntax)(Function(asClause) asClause.AsKeyword)
        End Function

        Private Sub PositionOutsideTupleIfApplicable(syntaxTree As SyntaxTree, ByRef position As Integer,
                                                     ByRef token As SyntaxToken, cancellationToken As CancellationToken)

            While syntaxTree.IsPossibleTupleContext(token, position)
                Dim possibleTuple = token.Parent
                position = possibleTuple.FullSpan.Start
                token = syntaxTree.GetTargetToken(position, cancellationToken)
            End While
        End Sub

        <Extension()>
        Public Function IsNameOfContext(syntaxTree As SyntaxTree, position As Integer, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            ' first do quick exit check
            If syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) OrElse
               syntaxTree.IsInInactiveRegion(position, cancellationToken) OrElse
               syntaxTree.IsEntirelyWithinComment(position, cancellationToken) OrElse
               syntaxTree.IsEntirelyWithinStringOrCharOrNumericLiteral(position, cancellationToken) Then

                Return False
            End If

            Return syntaxTree _
                .GetTargetToken(position, cancellationToken) _
                .IsChildToken(Of NameOfExpressionSyntax)(Function(nameOfExpression) nameOfExpression.OpenParenToken)
        End Function

        <Extension()>
        Friend Function IsSingleLineStatementContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)
            Return IsSingleLineStatementContext(syntaxTree, position, targetToken, cancellationToken)
        End Function

        ''' <summary>
        ''' The specified position is where I could start a statement in a place where exactly one
        ''' statement could exist.
        ''' </summary>
        <Extension()>
        Friend Function IsSingleLineStatementContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            ' We can always put single-statement constructs anywhere we can do a multi-statement
            ' construct
            If syntaxTree.IsMultiLineStatementStartContext(position, targetToken, cancellationToken) Then
                Return True
            End If

            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            ' We might be after a single-line statement lambda
            Dim statementLambdaHeader = targetToken.GetAncestor(Of LambdaHeaderSyntax)()
            If statementLambdaHeader IsNot Nothing AndAlso statementLambdaHeader.Parent.IsKind(SyntaxKind.SingleLineSubLambdaExpression,
                                                                                                    SyntaxKind.MultiLineSubLambdaExpression) Then
                Return statementLambdaHeader.ParameterList Is Nothing AndAlso targetToken = statementLambdaHeader.DeclarationKeyword OrElse
                       statementLambdaHeader.ParameterList IsNot Nothing AndAlso targetToken = statementLambdaHeader.ParameterList.CloseParenToken
            End If

            Return False
        End Function

        ' PERF: Use UShort instead of SyntaxKind so the compiler can use array literal initialization.
        Private ReadOnly s_multilineStatementBlockStartKinds As SyntaxKind() = DirectCast(New UShort() {
            SyntaxKind.MultiLineFunctionLambdaExpression,
            SyntaxKind.MultiLineSubLambdaExpression,
            SyntaxKind.SubBlock,
            SyntaxKind.FunctionBlock,
            SyntaxKind.GetAccessorBlock,
            SyntaxKind.SetAccessorBlock,
            SyntaxKind.AddHandlerAccessorBlock,
            SyntaxKind.RemoveHandlerAccessorBlock,
            SyntaxKind.RaiseEventAccessorBlock,
            SyntaxKind.ConstructorBlock,
            SyntaxKind.OperatorBlock
        }, SyntaxKind())

        ''' <summary>
        ''' The specified position is where I could start a statement in a place where one or more
        ''' statements could exist.
        ''' </summary>
        <Extension()>
        Friend Function IsMultiLineStatementStartContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            If syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken) OrElse
               syntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return False
            End If

            ' There is one interesting exception to the code below: if it's the first statement inside
            ' a select case, then we can never have an executable statement
            If syntaxTree.IsStartOfSelectCaseBlock(position, targetToken, cancellationToken) Then
                Return False
            End If

            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.Kind = SyntaxKind.None Then
                Return False
            End If

            ' We might be after the Then or Else of a single-line if statement
            Dim singleLineIf = targetToken.GetAncestor(Of SingleLineIfStatementSyntax)()
            If singleLineIf IsNot Nothing AndAlso
              (targetToken.IsChildToken(Of SingleLineIfStatementSyntax)(Function(n) n.ThenKeyword) OrElse
               targetToken.IsChildToken(Of SingleLineElseClauseSyntax)(Function(n) n.ElseKeyword)) Then

                Return True
            End If

            If Not targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            Return syntaxTree.IsInStatementBlockOfKind(position,
                                                       targetToken,
                                                       cancellationToken,
                                                       s_multilineStatementBlockStartKinds)
        End Function

        <Extension()>
        Friend Function IsStartOfSelectCaseBlock(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Return syntaxTree.IsAfterStatementOfKind(position, token, cancellationToken, SyntaxKind.SelectStatement)
        End Function

        ''' <summary>
        ''' The specified position is immediately following a statement of one of the given kinds.
        ''' </summary>
        <Extension()>
        Friend Function IsAfterStatementOfKind(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken, ParamArray kinds As SyntaxKind()) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.Kind = SyntaxKind.None OrElse targetToken.Parent Is Nothing Then
                Return False
            End If

            If Not targetToken.FollowsEndOfStatement(position) Then
                Return False
            End If

            Return targetToken.GetAncestor(Of StatementSyntax).IsKind(kinds)
        End Function

        <Extension()>
        Friend Function IsInStatementBlockOfKind(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken, ParamArray kinds As SyntaxKind()) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            Dim ancestor = targetToken.Parent

            Do While ancestor IsNot Nothing
                If TypeOf ancestor Is EndBlockStatementSyntax Then
                    ' If we're within the End Block, skip the block itself
                    ancestor = ancestor.Parent.Parent

                    If ancestor Is Nothing Then
                        Return False
                    End If
                End If

                If ancestor.IsKind(kinds) Then
                    Return True
                End If

                If TypeOf ancestor Is LambdaExpressionSyntax Then
                    If Not (targetToken.FollowsEndOfStatement(position) AndAlso targetToken = ancestor.GetLastToken()) Then
                        ' We should not look past lambdas
                        Return False
                    End If
                End If

                ancestor = ancestor.Parent
            Loop

            Return False
        End Function

        <Extension()>
        Public Function IsQueryIntoClauseContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            If targetToken.Kind = SyntaxKind.None Then
                Return False
            End If

            If targetToken.Parent.FirstAncestorOrSelf(Of AggregateClauseSyntax)() Is Nothing AndAlso
               targetToken.Parent.FirstAncestorOrSelf(Of GroupByClauseSyntax)() Is Nothing AndAlso
               targetToken.Parent.FirstAncestorOrSelf(Of GroupJoinClauseSyntax)() Is Nothing Then
                Return False
            End If

            If targetToken.IsChildToken(Of AggregateClauseSyntax)(Function(a) a.IntoKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of AggregateClauseSyntax, AggregationRangeVariableSyntax)(Function(a) a.AggregationVariables) OrElse
               targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(g) g.IntoKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of GroupByClauseSyntax, AggregationRangeVariableSyntax)(Function(g) g.AggregationVariables) OrElse
               targetToken.IsChildToken(Of GroupJoinClauseSyntax)(Function(g) g.IntoKeyword) OrElse
               targetToken.IsChildSeparatorToken(Of GroupJoinClauseSyntax, AggregationRangeVariableSyntax)(Function(g) g.AggregationVariables) Then
                Return True
            End If

            If targetToken.Kind = SyntaxKind.EqualsToken Then
                Dim aggregationRangeVariable = targetToken.GetAncestor(Of AggregationRangeVariableSyntax)()
                If aggregationRangeVariable IsNot Nothing AndAlso aggregationRangeVariable.NameEquals IsNot Nothing Then
                    If aggregationRangeVariable.NameEquals.EqualsToken = targetToken Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsRaiseEventContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))
            Return Not targetToken.FollowsEndOfStatement(position) AndAlso targetToken.Kind = SyntaxKind.RaiseEventKeyword
        End Function

        <Extension()>
        Public Function IsObjectCreationTypeContext(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Dim targetToken = syntaxTree.GetTargetToken(position, cancellationToken)
            Return IsObjectCreationTypeContext(syntaxTree, position, targetToken, cancellationToken)
        End Function

        <Extension()>
        Public Function IsObjectCreationTypeContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))

            If Not targetToken.FollowsEndOfStatement(position) AndAlso targetToken.Kind = SyntaxKind.NewKeyword Then
                Return syntaxTree.IsTypeContext(position, targetToken, cancellationToken) OrElse
                       syntaxTree.IsMultiLineStatementStartContext(position, targetToken, cancellationToken) OrElse
                       syntaxTree.IsSingleLineStatementContext(position, targetToken, cancellationToken)
            End If

            Return False
        End Function

        <Extension>
        Friend Function IsEnumTypeMemberAccessContext(syntaxTree As SyntaxTree, position As Integer, targetToken As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))

            If Not targetToken.IsKind(SyntaxKind.DotToken) OrElse
               Not targetToken.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then

                Return False
            End If

            Dim memberAccess = DirectCast(targetToken.Parent, MemberAccessExpressionSyntax)

            Dim leftExpression = memberAccess.GetExpressionOfMemberAccessExpression()
            If leftExpression Is Nothing Then
                Return False
            End If

            Dim leftHandBinding = semanticModel.GetSymbolInfo(leftExpression)
            Dim symbol = leftHandBinding.GetBestOrAllSymbols().FirstOrDefault()

            If symbol Is Nothing Then
                Return False
            End If

            Select Case symbol.Kind
                Case SymbolKind.NamedType
                    Return DirectCast(symbol, INamedTypeSymbol).TypeKind = TypeKind.Enum
                Case SymbolKind.Alias
                    Dim target = DirectCast(symbol, IAliasSymbol).Target
                    Return target.IsType AndAlso DirectCast(target, ITypeSymbol).TypeKind = TypeKind.Enum
            End Select

            Return False
        End Function

        <Extension()>
        Friend Function IsFollowingCompleteExpression(Of TParent As SyntaxNode)(
            syntaxTree As SyntaxTree,
            position As Integer,
            targetToken As SyntaxToken,
            childGetter As Func(Of TParent, ExpressionSyntax),
            cancellationToken As CancellationToken,
            Optional allowImplicitLineContinuation As Boolean = True
        ) As Boolean

            Debug.Assert(targetToken = syntaxTree.GetTargetToken(position, cancellationToken))

            ' Check if our position begins a new statement
            If targetToken.MustBeginNewStatement(position) OrElse
                (targetToken.FollowsEndOfStatement(position) AndAlso Not allowImplicitLineContinuation) Then

                Return False
            End If

            For Each parent In targetToken.GetAncestors(Of TParent)()
                Dim expression = childGetter(parent)

                If expression Is Nothing Then
                    Continue For
                End If

                Dim terminatingToken = GetExpressionTerminatingToken(expression)
                If terminatingToken.Kind <> SyntaxKind.None AndAlso
                   Not terminatingToken.IsMissing AndAlso
                   terminatingToken = targetToken Then

                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Given a syntax node, this returns the token that is the "end" token that ends this
        ''' expression.
        ''' </summary>
        ''' <param name="expression">The expression to get the last token of.</param>
        ''' <returns>The last token, or SyntaxKind.None if the last token is missing.</returns>
        Friend Function GetExpressionTerminatingToken(expression As SyntaxNode) As SyntaxToken
            Dim parenthesizedExpression = TryCast(expression, ParenthesizedExpressionSyntax)
            If parenthesizedExpression IsNot Nothing Then
                Return parenthesizedExpression.CloseParenToken
            End If

            Dim literalExpression = TryCast(expression, LiteralExpressionSyntax)
            If literalExpression IsNot Nothing Then
                Return literalExpression.Token
            End If

            Dim binaryExpression = TryCast(expression, BinaryExpressionSyntax)
            If binaryExpression IsNot Nothing Then
                Return GetExpressionTerminatingToken(binaryExpression.Right)
            End If

            Dim invocationExpression = TryCast(expression, InvocationExpressionSyntax)
            If invocationExpression IsNot Nothing AndAlso invocationExpression.ArgumentList IsNot Nothing Then
                Return invocationExpression.ArgumentList.CloseParenToken
            End If

            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return memberAccessExpression.Name.Identifier
            End If

            Dim functionAggregationExpression = TryCast(expression, FunctionAggregationSyntax)
            If functionAggregationExpression IsNot Nothing Then
                If functionAggregationExpression.OpenParenToken.Kind <> SyntaxKind.None Then
                    Return functionAggregationExpression.CloseParenToken
                Else
                    Return functionAggregationExpression.FunctionName
                End If
            End If

            Dim identifierName = TryCast(expression, IdentifierNameSyntax)
            If identifierName IsNot Nothing Then
                Return identifierName.Identifier
            End If

            Dim predefinedType = TryCast(expression, PredefinedTypeSyntax)
            If predefinedType IsNot Nothing Then
                Return predefinedType.Keyword
            End If

            Dim collectionInitializer = TryCast(expression, CollectionInitializerSyntax)
            If collectionInitializer IsNot Nothing Then
                Return collectionInitializer.CloseBraceToken
            End If

            Dim objectCreation = TryCast(expression, ObjectCreationExpressionSyntax)
            If objectCreation IsNot Nothing Then
                If objectCreation.ArgumentList IsNot Nothing Then
                    Return objectCreation.ArgumentList.CloseParenToken
                ElseIf objectCreation.Type.IsKind(SyntaxKind.QualifiedName) Then
                    Return DirectCast(objectCreation.Type, QualifiedNameSyntax).Right.GetLastToken()
                Else
                    Return objectCreation.Type.GetLastToken()
                End If
            End If

            Dim arrayCreation = TryCast(expression, ArrayCreationExpressionSyntax)
            If arrayCreation IsNot Nothing Then
                Return arrayCreation.Initializer.CloseBraceToken
            End If

            Dim unaryExpression = TryCast(expression, UnaryExpressionSyntax)
            If unaryExpression IsNot Nothing Then
                Return GetExpressionTerminatingToken(unaryExpression.Operand)
            End If

            Dim queryExpression = TryCast(expression, QueryExpressionSyntax)
            If queryExpression IsNot Nothing Then
                Return GetQueryClauseTerminatingToken(queryExpression.Clauses.Last())
            End If

            Dim singleLineLambda = TryCast(expression, SingleLineLambdaExpressionSyntax)
            If singleLineLambda IsNot Nothing AndAlso singleLineLambda.Kind = SyntaxKind.SingleLineFunctionLambdaExpression Then
                Dim bodyExpression = TryCast(singleLineLambda.Body, ExpressionSyntax)
                If bodyExpression IsNot Nothing Then
                    Return GetExpressionTerminatingToken(bodyExpression)
                End If
            End If

            Dim multiLineLambda = TryCast(expression, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                If multiLineLambda.EndSubOrFunctionStatement IsNot Nothing Then
                    Return multiLineLambda.EndSubOrFunctionStatement.BlockKeyword
                End If
            End If

            Dim anonymousObjectCreation = TryCast(expression, AnonymousObjectCreationExpressionSyntax)
            If anonymousObjectCreation IsNot Nothing Then
                If anonymousObjectCreation.Initializer IsNot Nothing Then
                    Return anonymousObjectCreation.Initializer.CloseBraceToken
                End If
            End If

            ' A SyntaxTokenStruct with Kind = None
            Return Nothing
        End Function

        Private Function GetQueryClauseTerminatingToken(queryClause As QueryClauseSyntax) As SyntaxToken
            Dim fromClause = TryCast(queryClause, FromClauseSyntax)
            If fromClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(fromClause.Variables.LastCollectionExpression())
            End If

            Dim whereClause = TryCast(queryClause, WhereClauseSyntax)
            If whereClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(whereClause.Condition)
            End If

            Dim letClause = TryCast(queryClause, LetClauseSyntax)
            If letClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(letClause.Variables.LastRangeExpression())
            End If

            Dim orderByClause = TryCast(queryClause, OrderByClauseSyntax)
            If orderByClause IsNot Nothing Then
                Dim lastOrdering = orderByClause.Orderings.Last()

                If lastOrdering.AscendingOrDescendingKeyword.Kind = SyntaxKind.None Then
                    Return GetExpressionTerminatingToken(lastOrdering.Expression)
                Else
                    Return lastOrdering.AscendingOrDescendingKeyword
                End If
            End If

            Dim partitionWhileClause = TryCast(queryClause, PartitionWhileClauseSyntax)
            If partitionWhileClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(partitionWhileClause.Condition)
            End If

            Dim partitionClause = TryCast(queryClause, PartitionClauseSyntax)
            If partitionClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(partitionClause.Count)
            End If

            Dim aggregateClause = TryCast(queryClause, AggregateClauseSyntax)
            If aggregateClause IsNot Nothing Then
                If aggregateClause.AdditionalQueryOperators.Any() Then
                    Return GetQueryClauseTerminatingToken(aggregateClause.AdditionalQueryOperators.Last())
                Else
                    Return GetExpressionTerminatingToken(aggregateClause.Variables.LastCollectionExpression())
                End If
            End If

            Dim groupJoinClause = TryCast(queryClause, GroupJoinClauseSyntax)
            If groupJoinClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(groupJoinClause.AggregationVariables.LastAggregation())
            End If

            Dim joinClause = TryCast(queryClause, SimpleJoinClauseSyntax)
            If joinClause IsNot Nothing Then
                Dim lastJoinCondition = joinClause.JoinConditions.LastOrDefault()

                If lastJoinCondition IsNot Nothing Then
                    Return GetExpressionTerminatingToken(lastJoinCondition.Right)
                Else
                    Return Nothing
                End If
            End If

            Dim groupByClause = TryCast(queryClause, GroupByClauseSyntax)
            If groupByClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(groupByClause.AggregationVariables.LastAggregation())
            End If

            Dim selectClause = TryCast(queryClause, SelectClauseSyntax)
            If selectClause IsNot Nothing Then
                Return GetExpressionTerminatingToken(selectClause.Variables.LastRangeExpression())
            End If

            Dim distinctClause = TryCast(queryClause, DistinctClauseSyntax)
            If distinctClause IsNot Nothing Then
                Return distinctClause.DistinctKeyword
            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension()>
        Friend Function LastCollectionExpression(collection As SeparatedSyntaxList(Of CollectionRangeVariableSyntax)) As ExpressionSyntax
            Dim lastCollectionRange = collection.LastOrDefault()

            If lastCollectionRange IsNot Nothing Then
                Return lastCollectionRange.Expression
            Else
                Return Nothing
            End If
        End Function

        <Extension()>
        Friend Function LastRangeExpression(collection As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax)) As ExpressionSyntax
            Dim lastCollectionRange = collection.LastOrDefault()

            If lastCollectionRange IsNot Nothing Then
                Return lastCollectionRange.Expression
            Else
                Return Nothing
            End If
        End Function

        <Extension()>
        Friend Function LastAggregation(collection As SeparatedSyntaxList(Of AggregationRangeVariableSyntax)) As AggregationSyntax
            Dim lastCollectionRange = collection.LastOrDefault()

            If lastCollectionRange IsNot Nothing Then
                Return lastCollectionRange.Aggregation
            Else
                Return Nothing
            End If
        End Function

        ' Tuple literals aren't recognized by the parser until there is a comma
        ' So a parenthesized expression is a possible tuple context too
        <Extension>
        Friend Function IsPossibleTupleContext(syntaxTree As SyntaxTree,
                                               tokenOnLeftOfPosition As SyntaxToken,
                                               position As Integer) As Boolean

            tokenOnLeftOfPosition = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position)

            If tokenOnLeftOfPosition.IsKind(SyntaxKind.OpenParenToken) Then
                Return tokenOnLeftOfPosition.Parent.IsKind(SyntaxKind.ParenthesizedExpression,
                                                           SyntaxKind.TupleExpression, SyntaxKind.TupleType)
            End If

            Return tokenOnLeftOfPosition.IsKind(SyntaxKind.CommaToken) AndAlso
                tokenOnLeftOfPosition.Parent.IsKind(SyntaxKind.TupleExpression, SyntaxKind.TupleType)
        End Function

    End Module
End Namespace
