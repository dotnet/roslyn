' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports System.Composition
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(AbstractChangeSignatureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureService
        Inherits AbstractChangeSignatureService

        Private Shared ReadOnly _declarationKinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.SubStatement,
            SyntaxKind.FunctionStatement,
            SyntaxKind.SubNewStatement,
            SyntaxKind.PropertyStatement,
            SyntaxKind.DelegateSubStatement,
            SyntaxKind.DelegateFunctionStatement,
            SyntaxKind.EventStatement)

        Private Shared ReadOnly _declarationAndInvocableKinds As ImmutableArray(Of SyntaxKind) =
            _declarationKinds.Concat(ImmutableArray.Create(
                SyntaxKind.SubBlock,
                SyntaxKind.FunctionBlock,
                SyntaxKind.ConstructorBlock,
                SyntaxKind.PropertyBlock,
                SyntaxKind.InvocationExpression,
                SyntaxKind.EventBlock,
                SyntaxKind.ObjectCreationExpression))

        Private Shared ReadOnly _nodeKindsToIgnore As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.ImplementsClause)

        Private Shared ReadOnly _updatableNodeKinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.CrefReference,
            SyntaxKind.ImplementsClause,
            SyntaxKind.SubStatement,
            SyntaxKind.FunctionStatement,
            SyntaxKind.DelegateSubStatement,
            SyntaxKind.DelegateFunctionStatement,
            SyntaxKind.EventBlock,
            SyntaxKind.EventStatement,
            SyntaxKind.RaiseEventStatement,
            SyntaxKind.PropertyStatement,
            SyntaxKind.InvocationExpression,
            SyntaxKind.Attribute,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.SubNewStatement,
            SyntaxKind.ConstructorBlock,
            SyntaxKind.SingleLineSubLambdaExpression,
            SyntaxKind.MultiLineSubLambdaExpression,
            SyntaxKind.SingleLineFunctionLambdaExpression,
            SyntaxKind.MultiLineFunctionLambdaExpression)

        Private Shared ReadOnly _updatableAncestorKinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.CrefReference,
            SyntaxKind.ImplementsClause,
            SyntaxKind.SubStatement,
            SyntaxKind.FunctionStatement,
            SyntaxKind.DelegateSubStatement,
            SyntaxKind.DelegateFunctionStatement,
            SyntaxKind.EventBlock,
            SyntaxKind.EventStatement,
            SyntaxKind.RaiseEventStatement,
            SyntaxKind.PropertyStatement,
            SyntaxKind.InvocationExpression,
            SyntaxKind.Attribute,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.SubNewStatement,
            SyntaxKind.ConstructorBlock)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Async Function GetInvocationSymbolAsync(
                document As Document,
                position As Integer,
                restrictToDeclarations As Boolean,
                cancellationToken As CancellationToken) As Task(Of (symbol As ISymbol, selectedIndex As Integer))
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.GetRoot(cancellationToken).FindToken(If(position <> tree.Length, position, Math.Max(0, position - 1)))

            Dim matchingNode = GetMatchingNode(token.Parent, restrictToDeclarations)

            If matchingNode Is Nothing Then
                Return Nothing
            End If

            ' Don't show change-signature in the random whitespace/trivia for code.
            If Not matchingNode.Span.IntersectsWith(position) Then
                Return Nothing
            End If

            ' If we're actually on the declaration of some symbol, ensure that we're
            ' in a good location for that symbol (i.e. Not in the attributes or after the parameter list).
            If Not IsInSymbolHeader(matchingNode, position) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim symbol = TryGetDeclaredSymbol(semanticModel, matchingNode, cancellationToken)
            If symbol IsNot Nothing Then
                Dim selectedIndex = TryGetSelectedIndexFromDeclaration(position, matchingNode)
                Return (symbol, selectedIndex)
            End If

            If matchingNode.Kind() = SyntaxKind.ObjectCreationExpression Then
                Dim objectCreation = DirectCast(matchingNode, ObjectCreationExpressionSyntax)
                If token.Parent.AncestorsAndSelf().Any(Function(a) a Is objectCreation.Type) Then
                    Dim typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol
                    If typeSymbol IsNot Nothing AndAlso typeSymbol.IsKind(SymbolKind.NamedType) AndAlso DirectCast(typeSymbol, ITypeSymbol).TypeKind = TypeKind.Delegate Then
                        Return (typeSymbol, 0)
                    End If
                End If
            End If

            Dim symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken)
            Return (If(symbolInfo.Symbol, symbolInfo.CandidateSymbols.FirstOrDefault()), 0)
        End Function

        Private Function TryGetSelectedIndexFromDeclaration(position As Integer, matchingNode As SyntaxNode) As Integer
            Dim parameters = matchingNode.ChildNodes().OfType(Of ParameterListSyntax)().SingleOrDefault()
            Return If(parameters Is Nothing, 0, GetParameterIndex(parameters.Parameters, position))
        End Function

        Private Function GetMatchingNode(node As SyntaxNode, restrictToDeclarations As Boolean) As SyntaxNode
            Dim current = node
            While current IsNot Nothing
                If restrictToDeclarations Then
                    If _declarationKinds.Contains(current.Kind()) Then
                        Return current
                    End If
                Else
                    If _declarationAndInvocableKinds.Contains(current.Kind()) Then
                        Return current
                    End If
                End If

                current = current.Parent
            End While

            Return Nothing
        End Function

        Private Function IsInSymbolHeader(matchingNode As SyntaxNode, position As Integer) As Boolean
            ' Caret has to be after the attributes if the symbol has any.
            Dim lastAttributes = matchingNode.ChildNodes().LastOrDefault(
                Function(n) TypeOf n Is AttributeListSyntax)
            Dim start = If(lastAttributes?.GetLastToken().GetNextToken().SpanStart,
                           matchingNode.SpanStart)

            If position < start Then
                Return False
            End If

            Dim asClause = matchingNode.ChildNodes().LastOrDefault(Function(n) TypeOf n Is AsClauseSyntax)
            If asClause IsNot Nothing Then
                Return position <= asClause.FullSpan.End
            End If

            ' If the symbol has a parameter list, then the caret shouldn't be past the end of it.
            Dim parameterList = matchingNode.ChildNodes().LastOrDefault(
                Function(n) TypeOf n Is ParameterListSyntax)
            If parameterList IsNot Nothing Then
                Return position <= parameterList.FullSpan.End
            End If

            ' Case we haven't handled yet.  Just assume we're in the header.
            Return True
        End Function

        Private Function TryGetDeclaredSymbol(semanticModel As SemanticModel,
                                              matchingNode As SyntaxNode,
                                              cancellationToken As CancellationToken) As ISymbol
            Select Case matchingNode.Kind()
                Case SyntaxKind.PropertyBlock
                    Dim parameterList = DirectCast(matchingNode, PropertyBlockSyntax).PropertyStatement.ParameterList
                    Return If(parameterList IsNot Nothing AndAlso parameterList.Parameters.Any(),
semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken),
                        Nothing)
                Case SyntaxKind.PropertyStatement
                    Dim parameterList = DirectCast(matchingNode, PropertyStatementSyntax).ParameterList
                    Return If(parameterList IsNot Nothing AndAlso parameterList.Parameters.Any(),
semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken),
                        Nothing)
                Case SyntaxKind.SubBlock
                    Return semanticModel.GetDeclaredSymbol(DirectCast(matchingNode, MethodBlockSyntax).BlockStatement, cancellationToken)
                Case SyntaxKind.FunctionBlock
                    Return semanticModel.GetDeclaredSymbol(DirectCast(matchingNode, MethodBlockSyntax).BlockStatement, cancellationToken)
                Case SyntaxKind.ConstructorBlock
                    Return semanticModel.GetDeclaredSymbol(DirectCast(matchingNode, ConstructorBlockSyntax).BlockStatement, cancellationToken)
            End Select

            Return semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken)
        End Function

        Public Overrides Function FindNodeToUpdate(document As Document, node As SyntaxNode) As SyntaxNode
            Dim vbnode = DirectCast(node, VisualBasicSyntaxNode)

            If _updatableNodeKinds.Contains(node.Kind()) Then
                Return GetUpdatableNode(node)
            End If

            Dim matchingNode = node.AncestorsAndSelf().FirstOrDefault(Function(a) _updatableAncestorKinds.Contains(a.Kind()))
            If matchingNode Is Nothing Then
                Return Nothing
            End If

            Return GetNodeContainingTargetNode(node, matchingNode)
        End Function

        Private Function GetNodeContainingTargetNode(originalNode As SyntaxNode, matchingNode As SyntaxNode) As SyntaxNode
            If matchingNode.IsKind(SyntaxKind.InvocationExpression) Then
                Return If(
originalNode.AncestorsAndSelf().Any(Function(n) n Is DirectCast(matchingNode, InvocationExpressionSyntax).Expression) OrElse
                        originalNode Is DirectCast(matchingNode, InvocationExpressionSyntax).ArgumentList,
                    GetUpdatableNode(matchingNode),
                    Nothing)
            End If

            Dim nodeContainingOriginal = matchingNode

            If nodeContainingOriginal.IsKind(SyntaxKind.ObjectCreationExpression) Then
                nodeContainingOriginal = DirectCast(nodeContainingOriginal, ObjectCreationExpressionSyntax).Type
            End If

            Return If(originalNode.AncestorsAndSelf().Any(Function(n) n Is nodeContainingOriginal), GetUpdatableNode(matchingNode), Nothing)

        End Function

        Private Function GetUpdatableNode(matchingNode As SyntaxNode) As SyntaxNode
            If _nodeKindsToIgnore.Contains(matchingNode.Kind()) Then
                Return Nothing
            End If

            If matchingNode.IsKind(SyntaxKind.EventStatement) AndAlso matchingNode.IsParentKind(SyntaxKind.EventBlock) Then
                matchingNode = matchingNode.Parent
            End If

            Return matchingNode
        End Function

        Public Overrides Function ChangeSignature(document As Document, declarationSymbol As ISymbol, potentiallyUpdatedNode As SyntaxNode, originalNode As SyntaxNode, updatedSignature As SignatureChange, cancellationToken As CancellationToken) As SyntaxNode
            Dim vbnode = DirectCast(potentiallyUpdatedNode, VisualBasicSyntaxNode)

            If Not declarationSymbol.GetParameters().Any() Then
                Return vbnode
            End If

            If vbnode.IsKind(SyntaxKind.SubStatement) OrElse
               vbnode.IsKind(SyntaxKind.FunctionStatement) OrElse
               vbnode.IsKind(SyntaxKind.SubNewStatement) OrElse
               vbnode.IsKind(SyntaxKind.Attribute) OrElse
               vbnode.IsKind(SyntaxKind.PropertyStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) OrElse
               vbnode.IsKind(SyntaxKind.EventBlock) OrElse
               vbnode.IsKind(SyntaxKind.EventStatement) Then

                Dim updatedLeadingTrivia = UpdateParamNodesInLeadingTrivia(vbnode, declarationSymbol, updatedSignature)
                If updatedLeadingTrivia IsNot Nothing Then
                    vbnode = vbnode.WithLeadingTrivia(updatedLeadingTrivia)
                End If
            End If

            If vbnode.IsKind(SyntaxKind.SubStatement) OrElse vbnode.IsKind(SyntaxKind.FunctionStatement) Then
                Dim method = DirectCast(vbnode, MethodStatementSyntax)
                Dim parameterList = method.ParameterList
                Dim updatedParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Return method.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                method.ParameterList.WithParameters(updatedParameters.permutedList).WithCloseParenToken(updatedParameters.closeParenToken),
                                                changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.EventStatement) Then
                Dim eventStatement = DirectCast(vbnode, EventStatementSyntax)

                Dim parameterList = eventStatement.ParameterList
                If parameterList IsNot Nothing Then
                    Dim updatedParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                    eventStatement = eventStatement.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                                      parameterList.WithParameters(updatedParameters.permutedList).WithCloseParenToken(updatedParameters.closeParenToken),
                                                                      changeSignatureFormattingAnnotation))
                End If

                Return eventStatement
            End If

            If vbnode.IsKind(SyntaxKind.EventBlock) Then
                Dim eventBlock = DirectCast(vbnode, EventBlockSyntax)
                Dim eventBlockParameterList = eventBlock.EventStatement.ParameterList

                If eventBlockParameterList IsNot Nothing Then
                    Dim updatedParameters = PermuteDeclaration(eventBlockParameterList.Parameters, eventBlockParameterList.CloseParenToken, updatedSignature)
                    Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithParameterList(
                                                         AnnotationExtensions.WithAdditionalAnnotations(
                                                         eventBlockParameterList.WithParameters(updatedParameters.permutedList).WithCloseParenToken(updatedParameters.closeParenToken),
                                                         changeSignatureFormattingAnnotation)))
                End If

                Dim raiseEventAccessor = eventBlock.Accessors.FirstOrDefault(Function(a) a.IsKind(SyntaxKind.RaiseEventAccessorBlock))
                If raiseEventAccessor IsNot Nothing Then
                    Dim raiseEventAccessorParameterList = raiseEventAccessor.BlockStatement.ParameterList
                    If raiseEventAccessor.BlockStatement.ParameterList IsNot Nothing Then
                        Dim updatedParameters = PermuteDeclaration(raiseEventAccessorParameterList.Parameters, raiseEventAccessorParameterList.CloseParenToken, updatedSignature)
                        Dim updatedRaiseEventAccessor = raiseEventAccessor.WithAccessorStatement(
                            raiseEventAccessor.AccessorStatement.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                            raiseEventAccessor.AccessorStatement.ParameterList.WithParameters(updatedParameters.permutedList).WithCloseParenToken(updatedParameters.closeParenToken), changeSignatureFormattingAnnotation)))
                        eventBlock = eventBlock.WithAccessors(eventBlock.Accessors.Remove(raiseEventAccessor).Add(updatedRaiseEventAccessor))
                    End If
                End If

                Return eventBlock
            End If

            If vbnode.IsKind(SyntaxKind.RaiseEventStatement) Then
                Dim raiseEventStatement = DirectCast(vbnode, RaiseEventStatementSyntax)
                Dim argumentList = raiseEventStatement.ArgumentList
                Dim updatedArguments = PermuteArgumentList(argumentList.Arguments, argumentList.CloseParenToken, updatedSignature, declarationSymbol)
                Return raiseEventStatement.WithArgumentList(argumentList.WithArguments(
                                                            updatedArguments.permutedList).WithCloseParenToken(updatedArguments.closeParenToken).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.InvocationExpression) Then
                Dim invocation = DirectCast(vbnode, InvocationExpressionSyntax)
                Dim semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken)

                Dim isReducedExtensionMethod = False
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, InvocationExpressionSyntax))
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)
                If methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.ReducedExtension Then
                    isReducedExtensionMethod = True
                End If

                Dim argumentList As ArgumentListSyntax = invocation.ArgumentList
                Dim newArguments = PermuteArgumentList(argumentList.Arguments, argumentList.CloseParenToken, updatedSignature, declarationSymbol, isReducedExtensionMethod)
                Return invocation.WithArgumentList(argumentList.WithArguments(
                                                   newArguments.permutedList).WithCloseParenToken(newArguments.closeParenToken).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.SubNewStatement) Then
                Dim constructor = DirectCast(vbnode, SubNewStatementSyntax)
                Dim parameterList = constructor.ParameterList
                Dim newParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Return constructor.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                     constructor.ParameterList.WithParameters(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken),
                                                     changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.Attribute) Then
                Dim attribute = DirectCast(vbnode, AttributeSyntax)
                Dim argumentList = attribute.ArgumentList
                Dim newArguments = PermuteArgumentList(argumentList.Arguments, argumentList.CloseParenToken, updatedSignature, declarationSymbol)
                Return attribute.WithArgumentList(argumentList.WithArguments(
                                                  newArguments.permutedList).WithCloseParenToken(newArguments.closeParenToken).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Dim objectCreation = DirectCast(vbnode, ObjectCreationExpressionSyntax)
                Dim argumentList = objectCreation.ArgumentList
                Dim newArguments = PermuteArgumentList(argumentList.Arguments, argumentList.CloseParenToken, updatedSignature, declarationSymbol)
                Return objectCreation.WithArgumentList(argumentList.WithArguments(
                                                       newArguments.permutedList).WithCloseParenToken(newArguments.closeParenToken).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.PropertyStatement) Then
                Dim propertyStatement = DirectCast(vbnode, PropertyStatementSyntax)
                Dim parameterList = propertyStatement.ParameterList
                Dim newParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Return propertyStatement.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                           parameterList.WithParameters(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken),
                                                           changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.CrefReference) Then
                Dim crefReference = DirectCast(vbnode, CrefReferenceSyntax)

                If crefReference.Signature Is Nothing OrElse
                   Not crefReference.Signature.ArgumentTypes.Any() Then
                    Return crefReference
                End If

                Dim signature = crefReference.Signature
                Dim newParameters = PermuteDeclaration(signature.ArgumentTypes, signature.CloseParenToken, updatedSignature)
                Return crefReference.WithSignature(crefReference.Signature.WithArgumentTypes(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken))
            End If

            If vbnode.IsKind(SyntaxKind.SingleLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, SingleLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim parameterList = lambda.SubOrFunctionHeader.ParameterList
                Dim newParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                                            lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken),
                                                                            changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, MultiLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim parameterList = lambda.SubOrFunctionHeader.ParameterList
                Dim newParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                                            lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken),
                                                                            changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) Then
                Dim delegateStatement = DirectCast(vbnode, DelegateStatementSyntax)
                Dim parameterList = delegateStatement.ParameterList
                Dim newParameters = PermuteDeclaration(parameterList.Parameters, parameterList.CloseParenToken, updatedSignature)
                Return delegateStatement.WithParameterList(AnnotationExtensions.WithAdditionalAnnotations(
                                                           delegateStatement.ParameterList.WithParameters(newParameters.permutedList).WithCloseParenToken(newParameters.closeParenToken), changeSignatureFormattingAnnotation))
            End If

            Return vbnode
        End Function

        Private Function PermuteArgumentList(
            arguments As SeparatedSyntaxList(Of ArgumentSyntax),
            closeParenToken As SyntaxToken,
            permutedSignature As SignatureChange,
            declarationSymbol As ISymbol,
            Optional isReducedExtensionMethod As Boolean = False) As (permutedList As SeparatedSyntaxList(Of ArgumentSyntax), closeParenToken As SyntaxToken)
            Dim originalArguments = arguments.Select(Function(a) UnifiedArgumentSyntax.Create(a, arguments.IndexOf(a))).ToList()
            Dim permutedArguments = PermuteArguments(declarationSymbol, originalArguments, permutedSignature, isReducedExtensionMethod)

            Dim newArguments = New List(Of ArgumentSyntax)()
            Dim newSeparators = New SyntaxToken(arguments.Count - 1) {}
            For newIndex = 0 To permutedArguments.Count - 1
                Dim argument = permutedArguments(newIndex)
                Dim originalIndex = argument.Index

                Dim newParamNode = TransferLeadingTrivia(CType(DirectCast(argument, UnifiedArgumentSyntax), ArgumentSyntax), arguments(newIndex))
                newArguments.Add(newParamNode)

                closeParenToken = TransferSeparatorTrivia(arguments, closeParenToken, permutedArguments.Count, newSeparators, newIndex, originalIndex)
            Next

            Return (SyntaxFactory.SeparatedList(newArguments, newSeparators.ToList().GetRange(0, If(permutedArguments.Count = 0, 0, permutedArguments.Count - 1))), closeParenToken)
        End Function

        Private Function PermuteDeclaration(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T), closeParenToken As SyntaxToken, updatedSignature As SignatureChange) As (permutedList As SeparatedSyntaxList(Of T), closeParenToken As SyntaxToken)
            Dim originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters()
            Dim reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters()

            Dim newParameters = New List(Of T)()
            Dim newSeparators = New SyntaxToken(list.Count - 1) {}
            For newIndex = 0 To reorderedParameters.Count - 1
                Dim newParamSymbol = reorderedParameters(newIndex)
                Dim originalIndex = originalParameters.IndexOf(newParamSymbol)

                ' Copy whitespace trivia from original position.
                Dim newParamNode = TransferLeadingTrivia(list(originalIndex), list(newIndex))
                newParameters.Add(newParamNode)

                closeParenToken = TransferSeparatorTrivia(list, closeParenToken, reorderedParameters.Count, newSeparators, newIndex, originalIndex)
            Next

            Return (SyntaxFactory.SeparatedList(newParameters, newSeparators.ToList().GetRange(0, If(reorderedParameters.Count = 0, 0, reorderedParameters.Count - 1))), closeParenToken)
        End Function

        Private Shared Function TransferSeparatorTrivia(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T), closeParenToken As SyntaxToken, reorderedParameterCount As Integer, newSeparators() As SyntaxToken, newIndex As Integer, originalIndex As Integer) As SyntaxToken
            If newIndex <> reorderedParameterCount - 1 Then
                ' Update separator trivia if we're not at the last node.
                newSeparators(newIndex) = TransferTrivia(list, closeParenToken, originalIndex, newIndex, False)
            Else
                ' If we're at the last node, we instead append the original separator trivia to the close paren token.
                closeParenToken = TransferTrivia(list, closeParenToken, originalIndex, newIndex, True)
            End If

            Return closeParenToken
        End Function

        Private Shared Function TransferTrivia(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T), closeParenToken As SyntaxToken, originalIndex As Integer, newIndex As Integer, lastParam As Boolean) As SyntaxToken
            Dim originalSeparator = If(originalIndex = list.Count - 1, closeParenToken, list.GetSeparator(originalIndex))
            Dim newSeparator = If(lastParam, closeParenToken, list.GetSeparator(newIndex))

            ' If the associated node is not switching positions, we don't need to do any work.
            If originalIndex = newIndex Then
                Return newSeparator
            End If

            ' Transfer trivia from the original separator to new separator, excluding any end-of-line trivia.
            Dim triviaToTransfer = From trivia In originalSeparator.TrailingTrivia Where Not trivia.IsKind(SyntaxKind.EndOfLineTrivia)

            ' If our new separator is the close paren token and all the trivia from the original separator is whitespace trivia, we don't want to append any whitespace.
            If lastParam And triviaToTransfer.All(Function(trivia) trivia.IsKind(SyntaxKind.WhitespaceTrivia)) Then
                Return newSeparator
            End If

            ' Re-append end-of-line trivia, if any.
            triviaToTransfer = triviaToTransfer.Concat(newSeparator.TrailingTrivia.Where(Function(trivia) trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            newSeparator = newSeparator.WithTrailingTrivia(triviaToTransfer)

            ' If we're not at the last parameter and there's no space after the current separator, append a space.
            If (Not lastParam) And newSeparator.TrailingTrivia.IsEmpty Then
                newSeparator = newSeparator.WithTrailingTrivia(SyntaxFactory.ElasticWhitespace(" "))
            End If

            ' Does the new separator have a comment? If so, make sure we have a new line at the end, and append a new line if there isn't one. We use the position of the previous node to determine the indentation.
            If newSeparator.TrailingTrivia.Contains(Function(trivia) trivia.IsKind(SyntaxKind.CommentTrivia)) And Not newSeparator.TrailingTrivia.Contains(Function(trivia) trivia.IsKind(SyntaxKind.EndOfLineTrivia)) Then
                Dim location = list(newIndex).GetLocation()

                If Not location.Kind = LocationKind.None Then
                    Dim previousNodeLeadingTriviaSpanStart = location.GetLineSpan().StartLinePosition.Character
                    Dim leadingWhitespace = ""
                    For i As Integer = 0 To previousNodeLeadingTriviaSpanStart - 1
                        leadingWhitespace += " "
                    Next

                    newSeparator = newSeparator.WithAppendedTrailingTrivia(SyntaxFactory.ElasticEndOfLine(vbCrLf))
                    newSeparator = newSeparator.WithAppendedTrailingTrivia(SyntaxFactory.ElasticWhitespace(leadingWhitespace))
                End If
            End If

            Return newSeparator
        End Function

        Private Shared Function TransferLeadingTrivia(Of T As SyntaxNode)(newArgument As T, oldArgument As SyntaxNode) As T
            Dim oldTrivia = oldArgument.GetLeadingTrivia()
            Return newArgument.WithLeadingTrivia(oldTrivia)
        End Function

        Private Function UpdateParamNodesInLeadingTrivia(node As VisualBasicSyntaxNode, declarationSymbol As ISymbol, updatedSignature As SignatureChange) As List(Of SyntaxTrivia)
            If Not node.HasLeadingTrivia Then
                Return Nothing
            End If

            Dim paramNodes = node _
                .DescendantNodes(descendIntoTrivia:=True) _
                .OfType(Of XmlElementSyntax)() _
                .Where(Function(e) e.StartTag.Name.ToString() = DocumentationCommentXmlNames.ParameterElementName)

            Dim permutedParamNodes = VerifyAndPermuteParamNodes(paramNodes, declarationSymbol, updatedSignature)
            If permutedParamNodes Is Nothing Then
                ' Something is wrong with the <param> tags, so don't change anything.
                Return Nothing
            End If

            Return GetPermutedTrivia(node, permutedParamNodes)
        End Function

        Private Function VerifyAndPermuteParamNodes(paramNodes As IEnumerable(Of XmlElementSyntax), declarationSymbol As ISymbol, updatedSignature As SignatureChange) As List(Of XmlElementSyntax)
            ' Only reorder if count and order match originally.

            Dim originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters()
            Dim reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters()

            Dim declaredParameters = declarationSymbol.GetParameters()
            If paramNodes.Count() <> declaredParameters.Length Then
                Return Nothing
            End If

            Dim dictionary = New Dictionary(Of String, XmlElementSyntax)()
            Dim i = 0
            For Each paramNode In paramNodes
                Dim nameAttribute = paramNode.StartTag.Attributes.OfType(Of XmlNameAttributeSyntax).FirstOrDefault(Function(a) a.Name.ToString() = "name")
                If nameAttribute Is Nothing Then
                    Return Nothing
                End If

                Dim identifier = nameAttribute.DescendantNodes(descendIntoTrivia:=True).OfType(Of IdentifierNameSyntax)().FirstOrDefault()
                If (identifier Is Nothing OrElse identifier.ToString() <> declaredParameters.ElementAt(i).Name) Then
                    Return Nothing
                End If

                dictionary.Add(originalParameters(i).Name.ToString(), paramNode)
                i += 1
            Next

            ' Everything lines up, so permute them.

            Dim permutedParams = New List(Of XmlElementSyntax)()

            For Each parameter In reorderedParameters
                permutedParams.Add(dictionary(parameter.Name))
            Next

            Return permutedParams
        End Function

        Private Function GetPermutedTrivia(node As VisualBasicSyntaxNode, permutedParamNodes As List(Of XmlElementSyntax)) As List(Of SyntaxTrivia)
            Dim updatedLeadingTrivia = New List(Of SyntaxTrivia)()
            Dim index = 0

            For Each trivia In node.GetLeadingTrivia()
                If Not trivia.HasStructure Then

                    updatedLeadingTrivia.Add(trivia)
                    Continue For
                End If

                Dim structuredTrivia = TryCast(trivia.GetStructure(), DocumentationCommentTriviaSyntax)
                If structuredTrivia Is Nothing Then
                    updatedLeadingTrivia.Add(trivia)
                    Continue For
                End If

                Dim updatedNodeList = New List(Of XmlNodeSyntax)()
                For Each content In structuredTrivia.Content
                    If Not content.IsKind(SyntaxKind.XmlElement) Then
                        updatedNodeList.Add(content)
                        Continue For
                    End If

                    Dim xmlElement = DirectCast(content, XmlElementSyntax)
                    If xmlElement.StartTag.Name.ToString() <> DocumentationCommentXmlNames.ParameterElementName Then
                        updatedNodeList.Add(content)
                        Continue For
                    End If

                    ' Found a param tag, so insert the next one from the reordered list.
                    If index < permutedParamNodes.Count Then
                        updatedNodeList.Add(permutedParamNodes(index).WithLeadingTrivia(content.GetLeadingTrivia()).WithTrailingTrivia(content.GetTrailingTrivia()))
                        index += 1
                    Else
                        ' Inspecting a param element that we are deleting but not replacing.
                    End If
                Next

                Dim newDocComments = SyntaxFactory.DocumentationCommentTrivia(SyntaxFactory.List(updatedNodeList.AsEnumerable()))
                newDocComments = newDocComments.WithLeadingTrivia(structuredTrivia.GetLeadingTrivia()).WithTrailingTrivia(structuredTrivia.GetTrailingTrivia())
                Dim newTrivia = SyntaxFactory.Trivia(newDocComments)

                updatedLeadingTrivia.Add(newTrivia)
            Next

            Return updatedLeadingTrivia
        End Function

        Public Overrides Async Function DetermineCascadedSymbolsFromDelegateInvoke(
                methodAndProjectId As SymbolAndProjectId(Of IMethodSymbol),
                document As Document,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndProjectId))

            Dim symbol = methodAndProjectId.Symbol
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim nodes = root.DescendantNodes()

            Dim results = ArrayBuilder(Of ISymbol).GetInstance()

            For Each n In nodes
                If n.IsKind(SyntaxKind.AddressOfExpression) Then
                    Dim u = DirectCast(n, UnaryExpressionSyntax)
                    Dim convertedType As ISymbol = semanticModel.GetTypeInfo(u).ConvertedType
                    If convertedType IsNot Nothing Then
                        convertedType = convertedType.OriginalDefinition
                    End If

                    If convertedType IsNot Nothing Then
                        convertedType = If(Await SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution).ConfigureAwait(False), convertedType)
                    End If

                    If Equals(convertedType, symbol.ContainingType) Then
                        convertedType = semanticModel.GetSymbolInfo(u.Operand).Symbol
                        If convertedType IsNot Nothing Then
                            results.Add(convertedType)
                        End If
                    End If
                ElseIf n.IsKind(SyntaxKind.EventStatement) Then
                    Dim cast = DirectCast(n, EventStatementSyntax)
                    If cast.AsClause IsNot Nothing Then
                        Dim nodeType = semanticModel.GetSymbolInfo(cast.AsClause.Type).Symbol

                        If nodeType IsNot Nothing Then
                            nodeType = nodeType.OriginalDefinition
                        End If

                        If nodeType IsNot Nothing Then
                            nodeType = If(Await SymbolFinder.FindSourceDefinitionAsync(nodeType, document.Project.Solution).ConfigureAwait(False), nodeType)
                        End If

                        If Equals(nodeType, symbol.ContainingType) Then
                            results.Add(semanticModel.GetDeclaredSymbol(cast.Identifier.Parent))
                        End If
                    End If
                End If
            Next

            Return results.ToImmutableAndFree().
                           SelectAsArray(Function(s) SymbolAndProjectId.Create(s, document.Project.Id))
        End Function
    End Class
End Namespace
