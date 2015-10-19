' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.ChangeSignature
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(AbstractChangeSignatureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureService
        Inherits AbstractChangeSignatureService

        Public Overrides Function GetInvocationSymbol(document As Document, position As Integer, restrictToDeclarations As Boolean, cancellationToken As CancellationToken) As ISymbol
            Dim tree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            Dim token = tree.GetRoot(cancellationToken).FindToken(If(position <> tree.Length, position, Math.Max(0, position - 1)))

            Dim matchingNode = token.Parent.AncestorsAndSelf().FirstOrDefault(Function(n) _invokableAncestorKinds.Contains(n.Kind))

            If matchingNode Is Nothing OrElse (restrictToDeclarations AndAlso _nonDeclarationKinds.Contains(matchingNode.Kind)) Then
                Return Nothing
            End If

            Dim semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken)

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
                Case SyntaxKind.ObjectCreationExpression
                    Dim objectCreation = DirectCast(matchingNode, ObjectCreationExpressionSyntax)
                    If token.Parent.AncestorsAndSelf().Any(Function(a) a Is objectCreation.Type) Then
                        Dim typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol
                        If typeSymbol IsNot Nothing AndAlso typeSymbol.IsKind(SymbolKind.NamedType) AndAlso DirectCast(typeSymbol, ITypeSymbol).TypeKind = TypeKind.Delegate Then
                            Return typeSymbol
                        End If
                    End If
            End Select

            Dim symbol = semanticModel.GetDeclaredSymbol(matchingNode, cancellationToken)
            If symbol IsNot Nothing Then
                Return symbol
            End If

            Dim symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken)
            Return If(symbolInfo.Symbol, symbolInfo.CandidateSymbols.FirstOrDefault())
        End Function

        Private _nonDeclarationKinds As ImmutableArray(Of SyntaxKind) = New List(Of SyntaxKind) From
            {
                SyntaxKind.SubBlock,
                SyntaxKind.FunctionBlock,
                SyntaxKind.PropertyBlock,
                SyntaxKind.EventBlock,
                SyntaxKind.ConstructorBlock
            }.ToImmutableArray()

        Private _invokableAncestorKinds As ImmutableArray(Of SyntaxKind) = New List(Of SyntaxKind) From
            {
                SyntaxKind.SubBlock,
                SyntaxKind.SubStatement,
                SyntaxKind.FunctionBlock,
                SyntaxKind.FunctionStatement,
                SyntaxKind.ConstructorBlock,
                SyntaxKind.SubNewStatement,
                SyntaxKind.PropertyBlock,
                SyntaxKind.PropertyStatement,
                SyntaxKind.InvocationExpression,
                SyntaxKind.DelegateSubStatement,
                SyntaxKind.DelegateFunctionStatement,
                SyntaxKind.EventStatement,
                SyntaxKind.EventBlock,
                SyntaxKind.ObjectCreationExpression
            }.ToImmutableArray()

        Private _nodeKindsToIgnore As ImmutableArray(Of SyntaxKind) = New List(Of SyntaxKind) From
            {
                SyntaxKind.ImplementsClause
            }.ToImmutableArray()

        Private _updatableNodeKinds As ImmutableArray(Of SyntaxKind) = New List(Of SyntaxKind) From
            {
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
                SyntaxKind.MultiLineFunctionLambdaExpression
            }.ToImmutableArray()

        Private _updatableAncestorKinds As ImmutableArray(Of SyntaxKind) = New List(Of SyntaxKind) From
            {
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
                SyntaxKind.ConstructorBlock
            }.ToImmutableArray()

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
                Dim updatedParameters = PermuteDeclaration(method.ParameterList.Parameters, updatedSignature)
                Return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.EventStatement) Then
                Dim eventStatement = DirectCast(vbnode, EventStatementSyntax)

                If eventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = PermuteDeclaration(eventStatement.ParameterList.Parameters, updatedSignature)
                    eventStatement = eventStatement.WithParameterList(eventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                End If

                Return eventStatement
            End If

            If vbnode.IsKind(SyntaxKind.EventBlock) Then
                Dim eventBlock = DirectCast(vbnode, EventBlockSyntax)

                If eventBlock.EventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = PermuteDeclaration(eventBlock.EventStatement.ParameterList.Parameters, updatedSignature)
                    Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithParameterList(eventBlock.EventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation)))
                End If

                Dim raiseEventAccessor = eventBlock.Accessors.FirstOrDefault(Function(a) a.IsKind(SyntaxKind.RaiseEventAccessorBlock))
                If raiseEventAccessor IsNot Nothing Then
                    If raiseEventAccessor.BlockStatement.ParameterList IsNot Nothing Then
                        Dim updatedParameters = PermuteDeclaration(raiseEventAccessor.BlockStatement.ParameterList.Parameters, updatedSignature)
                        Dim updatedRaiseEventAccessor = raiseEventAccessor.WithAccessorStatement(raiseEventAccessor.AccessorStatement.WithParameterList(raiseEventAccessor.AccessorStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation)))
                        eventBlock = eventBlock.WithAccessors(eventBlock.Accessors.Remove(raiseEventAccessor).Add(updatedRaiseEventAccessor))
                    End If
                End If

                Return eventBlock
            End If

            If vbnode.IsKind(SyntaxKind.RaiseEventStatement) Then
                Dim raiseEventStatement = DirectCast(vbnode, RaiseEventStatementSyntax)
                Dim updatedArguments = PermuteArgumentList(raiseEventStatement.ArgumentList.Arguments, updatedSignature, document, declarationSymbol)
                Return raiseEventStatement.WithArgumentList(raiseEventStatement.ArgumentList.WithArguments(updatedArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.InvocationExpression) Then
                Dim invocation = DirectCast(vbnode, InvocationExpressionSyntax)
                Dim semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken)

                Dim isReducedExtensionMethod = False
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, InvocationExpressionSyntax))
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)
                If methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.ReducedExtension Then
                    isReducedExtensionMethod = True
                End If

                Dim newArguments = PermuteArgumentList(invocation.ArgumentList.Arguments, updatedSignature, document, declarationSymbol, isReducedExtensionMethod)
                Return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.SubNewStatement) Then
                Dim constructor = DirectCast(vbnode, SubNewStatementSyntax)
                Dim newParameters = PermuteDeclaration(constructor.ParameterList.Parameters, updatedSignature)
                Return constructor.WithParameterList(constructor.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.Attribute) Then
                Dim attribute = DirectCast(vbnode, AttributeSyntax)
                Dim newArguments = PermuteArgumentList(attribute.ArgumentList.Arguments, updatedSignature, document, declarationSymbol)
                Return attribute.WithArgumentList(attribute.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Dim objectCreation = DirectCast(vbnode, ObjectCreationExpressionSyntax)
                Dim newArguments = PermuteArgumentList(objectCreation.ArgumentList.Arguments, updatedSignature, document, declarationSymbol)
                Return objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.PropertyStatement) Then
                Dim propertyStatement = DirectCast(vbnode, PropertyStatementSyntax)
                Dim newParameters = PermuteDeclaration(propertyStatement.ParameterList.Parameters, updatedSignature)
                Return propertyStatement.WithParameterList(propertyStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.CrefReference) Then
                Dim crefReference = DirectCast(vbnode, CrefReferenceSyntax)

                If crefReference.Signature Is Nothing OrElse
                   Not crefReference.Signature.ArgumentTypes.Any() Then
                    Return crefReference
                End If

                Dim newParameters = PermuteDeclaration(crefReference.Signature.ArgumentTypes, updatedSignature)
                Return crefReference.WithSignature(crefReference.Signature.WithArgumentTypes(newParameters))
            End If

            If vbnode.IsKind(SyntaxKind.SingleLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, SingleLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim newParameters = PermuteDeclaration(lambda.SubOrFunctionHeader.ParameterList.Parameters, updatedSignature)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, MultiLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim newParameters = PermuteDeclaration(lambda.SubOrFunctionHeader.ParameterList.Parameters, updatedSignature)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) Then
                Dim delegateStatement = DirectCast(vbnode, DelegateStatementSyntax)
                Dim newParameters = PermuteDeclaration(delegateStatement.ParameterList.Parameters, updatedSignature)
                Return delegateStatement.WithParameterList(delegateStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(changeSignatureFormattingAnnotation))
            End If

            Return vbnode
        End Function

        Private Function PermuteArgumentList(
            arguments As SeparatedSyntaxList(Of ArgumentSyntax),
            permutedSignature As SignatureChange,
            document As Document,
            declarationSymbol As ISymbol,
            Optional isReducedExtensionMethod As Boolean = False) As SeparatedSyntaxList(Of ArgumentSyntax)

            Dim newArguments As List(Of IUnifiedArgumentSyntax) = MyBase.PermuteArguments(document, declarationSymbol, arguments.Select(Function(a) UnifiedArgumentSyntax.Create(a)).ToList(), permutedSignature, isReducedExtensionMethod)

            Dim numSeparatorsToSkip = arguments.Count - newArguments.Count
            Return SyntaxFactory.SeparatedList(newArguments.Select(Function(a) CType(DirectCast(a, UnifiedArgumentSyntax), ArgumentSyntax)), GetSeparators(arguments, numSeparatorsToSkip))
        End Function

        Private Function PermuteDeclaration(Of T As SyntaxNode)(list As SeparatedSyntaxList(Of T), updatedSignature As SignatureChange) As SeparatedSyntaxList(Of T)
            Dim originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters()
            Dim reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters()

            Dim newParameters = New List(Of T)
            For Each newParam In reorderedParameters
                Dim pos = originalParameters.IndexOf(newParam)
                Dim param = list(pos)
                newParameters.Add(param)
            Next

            Dim numSeparatorsToSkip = originalParameters.Count - reorderedParameters.Count
            Return SyntaxFactory.SeparatedList(newParameters, GetSeparators(list, numSeparatorsToSkip))
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
            If paramNodes.Count() <> declaredParameters.Count() Then
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

        Private Shared Function GetSeparators(Of T As SyntaxNode)(arguments As SeparatedSyntaxList(Of T), Optional numSeparatorsToSkip As Integer = 0) As List(Of SyntaxToken)
            Dim separators = New List(Of SyntaxToken)
            For i = 0 To arguments.SeparatorCount - 1 - numSeparatorsToSkip
                separators.Add(arguments.GetSeparator(i))
            Next

            Return separators
        End Function

        Public Overrides Async Function DetermineCascadedSymbolsFromDelegateInvoke(symbol As IMethodSymbol, document As Document, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol))
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim nodes = root.DescendantNodes()

            Dim results = New List(Of ISymbol)

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

                    If convertedType Is symbol.ContainingType Then
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

                        If nodeType Is symbol.ContainingType Then
                            results.Add(semanticModel.GetDeclaredSymbol(cast.Identifier.Parent))
                        End If
                    End If
                End If
            Next

            Return results
        End Function

        Protected Overrides Function GetFormattingRules(document As Document) As IEnumerable(Of IFormattingRule)
            Return New IFormattingRule() {New ChangeSignatureFormattingRule()}.Concat(Formatter.GetDefaultFormattingRules(document))
        End Function
    End Class
End Namespace
