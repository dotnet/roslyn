' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        Dim s_createNewParameterSyntaxDelegate As Func(Of AddedParameter, ParameterSyntax) = AddressOf CreateNewParameterSyntax
        Dim s_createNewCrefParameterSyntaxDelegate As Func(Of AddedParameter, CrefSignaturePartSyntax) = AddressOf CreateNewCrefParameterSyntax

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
            Dim symbol = TryGetDeclaredSymbol(semanticModel, matchingNode, token, cancellationToken)
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

        Protected Overrides Function TryGetInsertPositionFromDeclaration(matchingNode As SyntaxNode) As Integer?
            Dim parameters = matchingNode.ChildNodes().OfType(Of ParameterListSyntax)().SingleOrDefault()

            If parameters Is Nothing Then
                Return Nothing
            End If

            Return parameters.CloseParenToken.SpanStart
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
                                              token As SyntaxToken,
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

        Public Overrides Async Function ChangeSignatureAsync(document As Document, declarationSymbol As ISymbol, potentiallyUpdatedNode As SyntaxNode, originalNode As SyntaxNode, updatedSignature As SignatureChange, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim vbnode = DirectCast(potentiallyUpdatedNode, VisualBasicSyntaxNode)
            If vbnode.IsKind(SyntaxKind.SubStatement) OrElse
               vbnode.IsKind(SyntaxKind.FunctionStatement) OrElse
               vbnode.IsKind(SyntaxKind.SubNewStatement) OrElse
               vbnode.IsKind(SyntaxKind.Attribute) OrElse
               vbnode.IsKind(SyntaxKind.PropertyStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) OrElse
               vbnode.IsKind(SyntaxKind.EventBlock) OrElse
               vbnode.IsKind(SyntaxKind.EventStatement) Then

                Dim updatedLeadingTrivia = UpdateParamNodesInLeadingTrivia(document, vbnode, declarationSymbol, updatedSignature)
                If updatedLeadingTrivia IsNot Nothing Then
                    vbnode = vbnode.WithLeadingTrivia(updatedLeadingTrivia)
                End If
            End If

            If vbnode.IsKind(SyntaxKind.SubStatement) OrElse vbnode.IsKind(SyntaxKind.FunctionStatement) Then
                Dim method = DirectCast(vbnode, MethodStatementSyntax)
                Dim updatedParameters = UpdateDeclaration(method.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.EventStatement) Then
                Dim eventStatement = DirectCast(vbnode, EventStatementSyntax)

                If eventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = UpdateDeclaration(eventStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                    eventStatement = eventStatement.WithParameterList(eventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                End If

                Return eventStatement
            End If

            If vbnode.IsKind(SyntaxKind.EventBlock) Then
                Dim eventBlock = DirectCast(vbnode, EventBlockSyntax)

                If eventBlock.EventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = UpdateDeclaration(eventBlock.EventStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                    Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithParameterList(eventBlock.EventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation)))
                End If

                Dim raiseEventAccessor = eventBlock.Accessors.FirstOrDefault(Function(a) a.IsKind(SyntaxKind.RaiseEventAccessorBlock))
                If raiseEventAccessor IsNot Nothing Then
                    If raiseEventAccessor.BlockStatement.ParameterList IsNot Nothing Then
                        Dim updatedParameters = UpdateDeclaration(raiseEventAccessor.BlockStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                        Dim updatedRaiseEventAccessor = raiseEventAccessor.WithAccessorStatement(raiseEventAccessor.AccessorStatement.WithParameterList(raiseEventAccessor.AccessorStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation)))
                        eventBlock = eventBlock.WithAccessors(eventBlock.Accessors.Remove(raiseEventAccessor).Add(updatedRaiseEventAccessor))
                    End If
                End If

                Return eventBlock
            End If

            If vbnode.IsKind(SyntaxKind.RaiseEventStatement) Then
                Dim raiseEventStatement = DirectCast(vbnode, RaiseEventStatementSyntax)
                Dim updatedArguments = PermuteArgumentList(raiseEventStatement.ArgumentList.Arguments, updatedSignature, declarationSymbol)
                Return raiseEventStatement.WithArgumentList(raiseEventStatement.ArgumentList.WithArguments(updatedArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.InvocationExpression) Then
                Dim invocation = DirectCast(vbnode, InvocationExpressionSyntax)
                Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

                Dim isReducedExtensionMethod = False
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, InvocationExpressionSyntax))
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)
                If methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.ReducedExtension Then
                    isReducedExtensionMethod = True
                End If

                Dim newArguments = PermuteArgumentList(invocation.ArgumentList.Arguments, updatedSignature.WithoutAddedParameters(), declarationSymbol, isReducedExtensionMethod)
                newArguments = AddNewArgumentsToList(newArguments, updatedSignature, isReducedExtensionMethod)
                Return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.SubNewStatement) Then
                Dim constructor = DirectCast(vbnode, SubNewStatementSyntax)
                Dim newParameters = UpdateDeclaration(constructor.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return constructor.WithParameterList(constructor.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.Attribute) Then
                Dim attribute = DirectCast(vbnode, AttributeSyntax)
                Dim newArguments = PermuteArgumentList(attribute.ArgumentList.Arguments, updatedSignature, declarationSymbol)
                Return attribute.WithArgumentList(attribute.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Dim objectCreation = DirectCast(vbnode, ObjectCreationExpressionSyntax)
                Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

                Dim isReducedExtensionMethod = False
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, ObjectCreationExpressionSyntax))
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)
                If methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.ReducedExtension Then
                    isReducedExtensionMethod = True
                End If

                Dim newArguments = PermuteArgumentList(objectCreation.ArgumentList.Arguments, updatedSignature.WithoutAddedParameters(), declarationSymbol, isReducedExtensionMethod)
                newArguments = AddNewArgumentsToList(newArguments, updatedSignature, isReducedExtensionMethod)
                Return objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.PropertyStatement) Then
                Dim propertyStatement = DirectCast(vbnode, PropertyStatementSyntax)
                Dim newParameters = UpdateDeclaration(propertyStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return propertyStatement.WithParameterList(propertyStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.CrefReference) Then
                Dim crefReference = DirectCast(vbnode, CrefReferenceSyntax)

                If crefReference.Signature Is Nothing OrElse
                   Not crefReference.Signature.ArgumentTypes.Any() Then
                    Return crefReference
                End If

                Dim newParameters = UpdateDeclaration(crefReference.Signature.ArgumentTypes, updatedSignature, s_createNewCrefParameterSyntaxDelegate)
                Return crefReference.WithSignature(crefReference.Signature.WithArgumentTypes(newParameters))
            End If

            If vbnode.IsKind(SyntaxKind.SingleLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, SingleLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim newParameters = UpdateDeclaration(lambda.SubOrFunctionHeader.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, MultiLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim newParameters = UpdateDeclaration(lambda.SubOrFunctionHeader.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) Then
                Dim delegateStatement = DirectCast(vbnode, DelegateStatementSyntax)
                Dim newParameters = UpdateDeclaration(delegateStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return delegateStatement.WithParameterList(delegateStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            Return vbnode
        End Function

        Private Function PermuteArgumentList(
            arguments As SeparatedSyntaxList(Of ArgumentSyntax),
            permutedSignature As SignatureChange,
            declarationSymbol As ISymbol,
            Optional isReducedExtensionMethod As Boolean = False) As SeparatedSyntaxList(Of ArgumentSyntax)

            Dim newArguments As List(Of IUnifiedArgumentSyntax) = MyBase.PermuteArguments(
                declarationSymbol, arguments.Select(Function(a) UnifiedArgumentSyntax.Create(a)).ToList(), permutedSignature,
                Function(callSiteValue) UnifiedArgumentSyntax.Create(SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression(callSiteValue))),
                isReducedExtensionMethod)

            Dim numSeparatorsToSkip As Integer
            If arguments.Count = 0 Then
                ' () 
                ' Adding X parameters, need to add X-1 separators.
                numSeparatorsToSkip = arguments.Count - newArguments.Count + 1
            Else
                ' (a,b,c)
                ' Adding X parameters, need to add X separators.
                numSeparatorsToSkip = arguments.Count - newArguments.Count
            End If

            Return SyntaxFactory.SeparatedList(newArguments.Select(Function(a) CType(DirectCast(a, UnifiedArgumentSyntax), ArgumentSyntax)), GetSeparators(arguments, numSeparatorsToSkip))
        End Function

        Private Function UpdateDeclaration(Of T As SyntaxNode)(
                parameterList As SeparatedSyntaxList(Of T),
                updatedSignature As SignatureChange,
                createNewParameterMethod As Func(Of AddedParameter, T)) As SeparatedSyntaxList(Of T)
            Dim updatedDeclaration = UpdateDeclarationBase(parameterList, updatedSignature, createNewParameterMethod)
            Return SyntaxFactory.SeparatedList(updatedDeclaration.parameters, updatedDeclaration.separators)
        End Function

        Private Shared Function CreateNewParameterSyntax(addedParameter As AddedParameter) As ParameterSyntax
            Return SyntaxFactory.Parameter(
                attributeLists:=SyntaxFactory.List(Of AttributeListSyntax)(),
                modifiers:=SyntaxFactory.TokenList(),
                identifier:=SyntaxFactory.ModifiedIdentifier(addedParameter.Name),
                asClause:=SyntaxFactory.SimpleAsClause(
                    addedParameter.Type.GenerateTypeSyntax() _
                    .WithPrependedLeadingTrivia(SyntaxFactory.ElasticSpace)) _
                    .WithPrependedLeadingTrivia(SyntaxFactory.ElasticSpace),
                [default]:=Nothing)
        End Function

        Private Shared Function CreateNewCrefParameterSyntax(addedParameter As AddedParameter) As CrefSignaturePartSyntax
            Return SyntaxFactory.CrefSignaturePart(
                modifier:=Nothing,
                type:=addedParameter.Type.GenerateTypeSyntax())
        End Function

        Private Function UpdateParamNodesInLeadingTrivia(document As Document, node As VisualBasicSyntaxNode, declarationSymbol As ISymbol, updatedSignature As SignatureChange) As List(Of SyntaxTrivia)
            If Not node.HasLeadingTrivia Then
                Return Nothing
            End If

            Dim paramNodes = node _
                .DescendantNodes(descendIntoTrivia:=True) _
                .OfType(Of XmlElementSyntax)() _
                .Where(Function(e) e.StartTag.Name.ToString() = DocumentationCommentXmlNames.ParameterElementName)

            Dim permutedParamNodes As List(Of SyntaxNode) = VerifyAndPermuteParamNodes(paramNodes, declarationSymbol, updatedSignature)
            If permutedParamNodes Is Nothing Then
                ' Something is wrong with the <param> tags, so don't change anything.
                Return Nothing
            End If

            Return GetPermutedDocCommentTrivia(document, node, permutedParamNodes)
        End Function

        Private Function VerifyAndPermuteParamNodes(paramNodes As IEnumerable(Of XmlElementSyntax), declarationSymbol As ISymbol, updatedSignature As SignatureChange) As List(Of SyntaxNode)
            ' Only reorder if count and order match originally.

            Dim originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters()
            Dim reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters()

            Dim declaredParameters = declarationSymbol.GetParameters()
            If paramNodes.Count() <> declaredParameters.Length Then
                Return Nothing
            End If

            If declaredParameters.Length = 0 Then
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
            Dim permutedParams = New List(Of SyntaxNode)()
            For Each parameter In reorderedParameters
                Dim permutedParam As XmlElementSyntax = Nothing
                If dictionary.TryGetValue(parameter.Name, permutedParam) Then
                    permutedParams.Add(permutedParam)
                Else
                    permutedParams.Add(SyntaxFactory.XmlElement(
                        SyntaxFactory.XmlElementStartTag(
                            SyntaxFactory.XmlName(Nothing, SyntaxFactory.XmlNameToken(DocumentationCommentXmlNames.ParameterElementName, SyntaxKind.XmlNameToken)),
                            SyntaxFactory.List(Of XmlNodeSyntax)({SyntaxFactory.XmlNameAttribute(parameter.Name)})),
                        SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(Nothing, SyntaxFactory.XmlNameToken(DocumentationCommentXmlNames.ParameterElementName, SyntaxKind.XmlNameToken)))))
                End If
            Next

            Return permutedParams
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

        Protected Overrides Function GetFormattingRules(document As Document) As IEnumerable(Of AbstractFormattingRule)
            Return SpecializedCollections.SingletonEnumerable(Of AbstractFormattingRule)(New ChangeSignatureFormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document))
        End Function

        Protected Overrides Function TransferLeadingWhitespaceTrivia(Of T As SyntaxNode)(newArgument As T, oldArgument As SyntaxNode) As T
            Return newArgument
        End Function

        Protected Overrides ReadOnly Property Generator As SyntaxGenerator
            Get
                Return VisualBasicSyntaxGenerator.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFactsService
            Get
                Return VisualBasicSyntaxFactsService.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

    End Class
End Namespace
