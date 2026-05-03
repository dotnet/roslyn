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
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

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

        Private ReadOnly s_createNewParameterSyntaxDelegate As Func(Of AddedParameter, ParameterSyntax) = AddressOf CreateNewParameterSyntax
        Private ReadOnly s_createNewCrefParameterSyntaxDelegate As Func(Of AddedParameter, CrefSignaturePartSyntax) = AddressOf CreateNewCrefParameterSyntax

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
                    Dim typeSymbol = semanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol
                    If typeSymbol IsNot Nothing AndAlso typeSymbol.IsKind(SymbolKind.NamedType) AndAlso DirectCast(typeSymbol, ITypeSymbol).TypeKind = TypeKind.Delegate Then
                        Return (typeSymbol, 0)
                    End If
                End If
            End If

            Dim symbolInfo = semanticModel.GetSymbolInfo(matchingNode, cancellationToken)
            symbol = If(symbolInfo.Symbol, symbolInfo.CandidateSymbols.FirstOrDefault())
            Dim parameterIndex = 0

            ' If we're being called on an invocation and not a definition we need to find the selected argument index based on the original definition.
            Dim invocation = matchingNode.GetAncestorOrThis(Of InvocationExpressionSyntax)
            Dim argument = invocation?.ArgumentList?.Arguments.FirstOrDefault(Function(a) a.Span.Contains(position))
            If (argument IsNot Nothing) Then
                parameterIndex = GetParameterIndexFromInvocationArgument(argument, document, semanticModel, cancellationToken)
            End If

            Return (symbol, parameterIndex)
        End Function

        Private Shared Function TryGetSelectedIndexFromDeclaration(position As Integer, matchingNode As SyntaxNode) As Integer
            Dim parameters = matchingNode.ChildNodes().OfType(Of ParameterListSyntax)().SingleOrDefault()
            Return If(parameters Is Nothing, 0, GetParameterIndex(parameters.Parameters, position))
        End Function

        Private Shared Function GetMatchingNode(node As SyntaxNode, restrictToDeclarations As Boolean) As SyntaxNode
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

        Private Shared Function IsInSymbolHeader(matchingNode As SyntaxNode, position As Integer) As Boolean
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

        Private Shared Function TryGetDeclaredSymbol(semanticModel As SemanticModel,
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

        Private Shared Function GetNodeContainingTargetNode(originalNode As SyntaxNode, matchingNode As SyntaxNode) As SyntaxNode
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

        Private Shared Function GetUpdatableNode(matchingNode As SyntaxNode) As SyntaxNode
            If _nodeKindsToIgnore.Contains(matchingNode.Kind()) Then
                Return Nothing
            End If

            If matchingNode.IsKind(SyntaxKind.EventStatement) AndAlso matchingNode.IsParentKind(SyntaxKind.EventBlock) Then
                matchingNode = matchingNode.Parent
            End If

            Return matchingNode
        End Function

        Public Overrides Function ChangeSignature(
            document As SemanticDocument,
            declarationSymbol As ISymbol,
            potentiallyUpdatedNode As SyntaxNode,
            originalNode As SyntaxNode,
            updatedSignature As SignatureChange,
            lineFormattingOptions As LineFormattingOptions,
            cancellationToken As CancellationToken) As SyntaxNode

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

                Dim updatedLeadingTrivia = UpdateParamNodesInLeadingTrivia(
                    document.Document, vbnode, declarationSymbol, updatedSignature, lineFormattingOptions)
                vbnode = vbnode.WithLeadingTrivia(updatedLeadingTrivia)
            End If

            If vbnode.IsKind(SyntaxKind.SubStatement) OrElse vbnode.IsKind(SyntaxKind.FunctionStatement) Then
                Dim method = DirectCast(vbnode, MethodStatementSyntax)
                Dim updatedParameters = UpdateDeclaration(method.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return method.WithParameterList(method.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.EventStatement) Then
                Dim eventStatement = DirectCast(vbnode, EventStatementSyntax)

                If eventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = UpdateDeclaration(eventStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                    eventStatement = eventStatement.WithParameterList(eventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
                End If

                Return eventStatement
            End If

            If vbnode.IsKind(SyntaxKind.EventBlock) Then
                Dim eventBlock = DirectCast(vbnode, EventBlockSyntax)

                If eventBlock.EventStatement.ParameterList IsNot Nothing Then
                    Dim updatedParameters = UpdateDeclaration(eventBlock.EventStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                    Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithParameterList(eventBlock.EventStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation)))
                End If

                Dim raiseEventAccessor = eventBlock.Accessors.FirstOrDefault(Function(a) a.IsKind(SyntaxKind.RaiseEventAccessorBlock))
                If raiseEventAccessor IsNot Nothing Then
                    If raiseEventAccessor.BlockStatement.ParameterList IsNot Nothing Then
                        Dim updatedParameters = UpdateDeclaration(raiseEventAccessor.BlockStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                        Dim updatedRaiseEventAccessor = raiseEventAccessor.WithAccessorStatement(raiseEventAccessor.AccessorStatement.WithParameterList(raiseEventAccessor.AccessorStatement.ParameterList.WithParameters(updatedParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation)))
                        eventBlock = eventBlock.WithAccessors(eventBlock.Accessors.Remove(raiseEventAccessor).Add(updatedRaiseEventAccessor))
                    End If
                End If

                Return eventBlock
            End If

            If vbnode.IsKind(SyntaxKind.RaiseEventStatement) Then
                Dim raiseEventStatement = DirectCast(vbnode, RaiseEventStatementSyntax)
                Dim semanticModel = document.SemanticModel
                Dim delegateInvokeMethod = DirectCast(DirectCast(semanticModel.GetSymbolInfo(raiseEventStatement.Name, cancellationToken).Symbol, IEventSymbol).Type, INamedTypeSymbol).DelegateInvokeMethod

                Return raiseEventStatement.WithArgumentList(UpdateArgumentList(
                    document,
                    delegateInvokeMethod,
                    updatedSignature,
                    raiseEventStatement.ArgumentList,
                    isReducedExtensionMethod:=False,
                    isParamsArrayExpanded:=False,
                    generateAttributeArguments:=False,
                    originalNode.SpanStart,
                    cancellationToken))
            End If

            If vbnode.IsKind(SyntaxKind.InvocationExpression) Then
                Dim invocation = DirectCast(vbnode, InvocationExpressionSyntax)
                Dim semanticModel = document.SemanticModel

                Dim isReducedExtensionMethod = False
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, InvocationExpressionSyntax), cancellationToken)
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)
                If methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.ReducedExtension Then
                    isReducedExtensionMethod = True
                End If

                If invocation.ArgumentList Is Nothing Then
                    ' If the invocation doesn't contain an argument list, we don't want to add one unless necessary.
                    ' In the case an argument list isn't needed, we can return early as there will be no changes to the invocation.
                    If updatedSignature.UpdatedConfiguration.ParametersWithoutDefaultValues.IsEmpty Then
                        Return invocation
                    Else
                        ' The invocation requires an argument list - add one.
                        Dim emptyArgumentList = SyntaxFactory.ArgumentList().WithTrailingTrivia(invocation.GetTrailingTrivia())
                        invocation = invocation.WithoutTrailingTrivia().WithArgumentList(emptyArgumentList)
                    End If
                End If

                Return invocation.WithArgumentList(UpdateArgumentList(
                    document,
                    declarationSymbol,
                    updatedSignature,
                    invocation.ArgumentList,
                    isReducedExtensionMethod,
                    IsParamsArrayExpanded(semanticModel, invocation, symbolInfo, cancellationToken),
                    generateAttributeArguments:=False,
                    originalNode.SpanStart,
                    cancellationToken))
            End If

            If vbnode.IsKind(SyntaxKind.SubNewStatement) Then
                Dim constructor = DirectCast(vbnode, SubNewStatementSyntax)
                Dim newParameters = UpdateDeclaration(constructor.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return constructor.WithParameterList(constructor.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
            End If

            If vbnode.IsKind(SyntaxKind.Attribute) Then
                Dim attribute = DirectCast(vbnode, AttributeSyntax)

                Dim semanticModel = document.SemanticModel
                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, AttributeSyntax), cancellationToken)
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)

                Return attribute.WithArgumentList(UpdateArgumentList(
                    document,
                    declarationSymbol,
                    updatedSignature,
                    attribute.ArgumentList,
                    isReducedExtensionMethod:=False,
                    isParamsArrayExpanded:=False,
                    generateAttributeArguments:=True,
                    originalNode.SpanStart,
                    cancellationToken))
            End If

            If vbnode.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Dim objectCreation = DirectCast(vbnode, ObjectCreationExpressionSyntax)
                Dim semanticModel = document.SemanticModel

                Dim symbolInfo = semanticModel.GetSymbolInfo(DirectCast(originalNode, ObjectCreationExpressionSyntax), cancellationToken)
                Dim methodSymbol = TryCast(symbolInfo.Symbol, IMethodSymbol)

                Dim paramsArrayExpanded = IsParamsArrayExpanded(semanticModel, objectCreation, symbolInfo, cancellationToken)

                Return objectCreation.WithArgumentList(UpdateArgumentList(
                    document,
                    declarationSymbol,
                    updatedSignature,
                    objectCreation.ArgumentList,
                    isReducedExtensionMethod:=False,
                    IsParamsArrayExpanded(semanticModel, objectCreation, symbolInfo, cancellationToken),
                    generateAttributeArguments:=False,
                    originalNode.SpanStart,
                    cancellationToken))
            End If

            If vbnode.IsKind(SyntaxKind.PropertyStatement) Then
                Dim propertyStatement = DirectCast(vbnode, PropertyStatementSyntax)
                Dim newParameters = UpdateDeclaration(propertyStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return propertyStatement.WithParameterList(propertyStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
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
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
               vbnode.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Dim lambda = DirectCast(vbnode, MultiLineLambdaExpressionSyntax)

                If Not lambda.SubOrFunctionHeader.ParameterList.Parameters.Any() Then
                    Return vbnode
                End If

                Dim newParameters = UpdateDeclaration(lambda.SubOrFunctionHeader.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Dim newBegin = lambda.SubOrFunctionHeader.WithParameterList(lambda.SubOrFunctionHeader.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
                Return lambda.WithSubOrFunctionHeader(newBegin)
            End If

            If vbnode.IsKind(SyntaxKind.DelegateSubStatement) OrElse
               vbnode.IsKind(SyntaxKind.DelegateFunctionStatement) Then
                Dim delegateStatement = DirectCast(vbnode, DelegateStatementSyntax)
                Dim newParameters = UpdateDeclaration(delegateStatement.ParameterList.Parameters, updatedSignature, s_createNewParameterSyntaxDelegate)
                Return delegateStatement.WithParameterList(delegateStatement.ParameterList.WithParameters(newParameters).WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation))
            End If

            Return vbnode
        End Function

        Private Function UpdateArgumentList(
            document As SemanticDocument,
            declarationSymbol As ISymbol,
            signaturePermutation As SignatureChange,
            argumentList As ArgumentListSyntax,
            isReducedExtensionMethod As Boolean,
            isParamsArrayExpanded As Boolean,
            generateAttributeArguments As Boolean,
            position As Integer,
            cancellationToken As CancellationToken) As ArgumentListSyntax

            Dim newArguments = PermuteArgumentList(
                argumentList.Arguments,
                signaturePermutation.WithoutAddedParameters(),
                declarationSymbol,
                isReducedExtensionMethod)

            newArguments = AddNewArgumentsToList(
                document,
                declarationSymbol,
                newArguments,
                signaturePermutation,
                isReducedExtensionMethod,
                isParamsArrayExpanded,
                generateAttributeArguments,
                position,
                cancellationToken)

            Return argumentList.
                WithArguments(newArguments).
                WithAdditionalAnnotations(ChangeSignatureFormattingAnnotation)
        End Function

        Private Shared Function IsParamsArrayExpanded(semanticModel As SemanticModel, node As SyntaxNode, symbolInfo As SymbolInfo, cancellationToken As CancellationToken) As Boolean
            If symbolInfo.Symbol Is Nothing Then
                Return False
            End If

            Dim argumentCount As Integer
            Dim lastArgumentIsNamed As Boolean
            Dim lastArgumentExpression As ExpressionSyntax = Nothing

            Dim invocation = TryCast(node, InvocationExpressionSyntax)
            Dim objectCreation = TryCast(node, ObjectCreationExpressionSyntax)
            If invocation IsNot Nothing Then
                GetArgumentListDetailsRegardingParamsArrays(invocation.ArgumentList, argumentCount, lastArgumentIsNamed, lastArgumentExpression)
            ElseIf objectCreation IsNot Nothing Then
                GetArgumentListDetailsRegardingParamsArrays(objectCreation.ArgumentList, argumentCount, lastArgumentIsNamed, lastArgumentExpression)
            Else
                Throw ExceptionUtilities.UnexpectedValue(node.Kind())
            End If

            Return IsParamsArrayExpandedHelper(symbolInfo.Symbol, argumentCount, lastArgumentIsNamed, semanticModel, lastArgumentExpression, cancellationToken)
        End Function

        Private Shared Sub GetArgumentListDetailsRegardingParamsArrays(
            argumentList As ArgumentListSyntax,
            ByRef argumentCount As Integer,
            ByRef lastArgumentIsNamed As Boolean,
            ByRef lastArgumentExpression As ExpressionSyntax)

            argumentCount = argumentList.Arguments.Count
            Dim isNamed = argumentList.Arguments.LastOrDefault()?.IsNamed
            lastArgumentIsNamed = isNamed.GetValueOrDefault()
            lastArgumentExpression = argumentList.Arguments.LastOrDefault()?.GetExpression()
        End Sub

        Private Function PermuteArgumentList(
            arguments As SeparatedSyntaxList(Of ArgumentSyntax),
            permutedSignature As SignatureChange,
            declarationSymbol As ISymbol,
            Optional isReducedExtensionMethod As Boolean = False) As SeparatedSyntaxList(Of ArgumentSyntax)

            Dim newArguments As ImmutableArray(Of IUnifiedArgumentSyntax) = PermuteArguments(
                declarationSymbol, arguments.Select(Function(a) UnifiedArgumentSyntax.Create(a)).ToImmutableArray(), permutedSignature,
                isReducedExtensionMethod)

            Dim numSeparatorsToSkip As Integer
            If arguments.Count = 0 Then
                ' () 
                ' Adding X parameters, need to add X-1 separators.
                numSeparatorsToSkip = arguments.Count - newArguments.Length + 1
            Else
                ' (a,b,c)
                ' Adding X parameters, need to add X separators.
                numSeparatorsToSkip = arguments.Count - newArguments.Length
            End If

            Return SeparatedList(newArguments.Select(Function(a) CType(DirectCast(a, UnifiedArgumentSyntax), ArgumentSyntax)), GetSeparators(arguments, numSeparatorsToSkip))
        End Function

        Private Function UpdateDeclaration(Of T As SyntaxNode)(
                parameterList As SeparatedSyntaxList(Of T),
                updatedSignature As SignatureChange,
                createNewParameterMethod As Func(Of AddedParameter, T)) As SeparatedSyntaxList(Of T)
            Dim updatedDeclaration = UpdateDeclarationBase(parameterList, updatedSignature, createNewParameterMethod)
            Return SeparatedList(updatedDeclaration.parameters, updatedDeclaration.separators)
        End Function

        Private Shared Function CreateNewParameterSyntax(addedParameter As AddedParameter) As ParameterSyntax
            Return SyntaxFactory.Parameter(
                attributeLists:=New SyntaxList(Of AttributeListSyntax)(),
                modifiers:=If(addedParameter.HasDefaultValue, TokenList(Token(SyntaxKind.OptionalKeyword)), TokenList()),
                identifier:=ModifiedIdentifier(addedParameter.Name),
                asClause:=SimpleAsClause(
                    addedParameter.Type.GenerateTypeSyntax() _
                    .WithPrependedLeadingTrivia(ElasticSpace)) _
                    .WithPrependedLeadingTrivia(ElasticSpace),
                [default]:=If(addedParameter.HasDefaultValue, EqualsValue(ParseExpression(addedParameter.DefaultValue)), Nothing))
        End Function

        Private Shared Function CreateNewCrefParameterSyntax(addedParameter As AddedParameter) As CrefSignaturePartSyntax
            Return CrefSignaturePart(
                modifier:=Nothing,
                type:=addedParameter.Type.GenerateTypeSyntax())
        End Function

        Private Function UpdateParamNodesInLeadingTrivia(
            document As Document,
            node As VisualBasicSyntaxNode,
            declarationSymbol As ISymbol,
            updatedSignature As SignatureChange,
            lineFormattingOption As LineFormattingOptions) As ImmutableArray(Of SyntaxTrivia)

            If Not node.HasLeadingTrivia Then
                Return ImmutableArray(Of SyntaxTrivia).Empty
            End If

            Dim paramNodes = node _
                .DescendantNodes(descendIntoTrivia:=True) _
                .OfType(Of XmlElementSyntax)() _
                .Where(Function(e) e.StartTag.Name.ToString() = DocumentationCommentXmlNames.ParameterElementName) _
                .ToImmutableArray()

            Dim permutedParamNodes = VerifyAndPermuteParamNodes(paramNodes, declarationSymbol, updatedSignature)
            If permutedParamNodes.IsEmpty() Then
                ' Something is wrong with the <param> tags, so don't change anything.
                Return node.GetLeadingTrivia().ToImmutableArray()
            End If

            Return GetPermutedDocCommentTrivia(node, permutedParamNodes, document.Project.Services, lineFormattingOption)
        End Function

        Private Function VerifyAndPermuteParamNodes(paramNodes As ImmutableArray(Of XmlElementSyntax), declarationSymbol As ISymbol, updatedSignature As SignatureChange) As ImmutableArray(Of SyntaxNode)
            ' Only reorder if count and order match originally.

            Dim originalParameters = updatedSignature.OriginalConfiguration.ToListOfParameters()
            Dim reorderedParameters = updatedSignature.UpdatedConfiguration.ToListOfParameters()

            Dim declaredParameters = GetParameters(declarationSymbol)
            If paramNodes.Length <> declaredParameters.Length Then
                Return ImmutableArray(Of SyntaxNode).Empty
            End If

            If declaredParameters.Length = 0 Then
                Return ImmutableArray(Of SyntaxNode).Empty
            End If

            Dim dictionary = New Dictionary(Of String, XmlElementSyntax)()
            Dim i = 0
            For Each paramNode In paramNodes
                Dim nameAttribute = paramNode.StartTag.Attributes.OfType(Of XmlNameAttributeSyntax).FirstOrDefault(Function(a) a.Name.ToString() = "name")
                If nameAttribute Is Nothing Then
                    Return ImmutableArray(Of SyntaxNode).Empty
                End If

                Dim identifier = nameAttribute.DescendantNodes(descendIntoTrivia:=True).OfType(Of IdentifierNameSyntax)().FirstOrDefault()
                If (identifier Is Nothing OrElse identifier.ToString() <> declaredParameters.ElementAt(i).Name) Then
                    Return ImmutableArray(Of SyntaxNode).Empty
                End If

                dictionary.Add(originalParameters(i).Name.ToString(), paramNode)
                i += 1
            Next

            ' Everything lines up, so permute them.
            Dim permutedParams = ArrayBuilder(Of SyntaxNode).GetInstance()
            For Each parameter In reorderedParameters
                Dim permutedParam As XmlElementSyntax = Nothing
                If dictionary.TryGetValue(parameter.Name, permutedParam) Then
                    permutedParams.Add(permutedParam)
                Else
                    permutedParams.Add(XmlElement(
                        XmlElementStartTag(
                            XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.ParameterElementName, SyntaxKind.XmlNameToken)),
                            List(Of XmlNodeSyntax)({XmlNameAttribute(parameter.Name)})),
                        XmlElementEndTag(XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.ParameterElementName, SyntaxKind.XmlNameToken)))))
                End If
            Next

            Return permutedParams.ToImmutableAndFree()
        End Function

        Public Overrides Async Function DetermineCascadedSymbolsFromDelegateInvokeAsync(
                method As IMethodSymbol,
                document As Document,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))

            Dim symbol = method
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim nodes = root.DescendantNodes()

            Dim results = ArrayBuilder(Of ISymbol).GetInstance()

            For Each n In nodes
                If n.IsKind(SyntaxKind.AddressOfExpression) Then
                    Dim u = DirectCast(n, UnaryExpressionSyntax)
                    Dim convertedType As ISymbol = semanticModel.GetTypeInfo(u, cancellationToken).ConvertedType
                    If convertedType IsNot Nothing Then
                        convertedType = convertedType.OriginalDefinition
                    End If

                    If convertedType IsNot Nothing Then
                        convertedType = If(Await SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).ConfigureAwait(False), convertedType)
                    End If

                    If Equals(convertedType, symbol.ContainingType) Then
                        convertedType = semanticModel.GetSymbolInfo(u.Operand, cancellationToken).Symbol
                        If convertedType IsNot Nothing Then
                            results.Add(convertedType)
                        End If
                    End If
                ElseIf n.IsKind(SyntaxKind.EventStatement) Then
                    Dim cast = DirectCast(n, EventStatementSyntax)
                    If cast.AsClause IsNot Nothing Then
                        Dim nodeType = semanticModel.GetSymbolInfo(cast.AsClause.Type, cancellationToken).Symbol

                        If nodeType IsNot Nothing Then
                            nodeType = nodeType.OriginalDefinition
                        End If

                        If nodeType IsNot Nothing Then
                            nodeType = If(Await SymbolFinder.FindSourceDefinitionAsync(nodeType, document.Project.Solution, cancellationToken).ConfigureAwait(False), nodeType)
                        End If

                        If Equals(nodeType, symbol.ContainingType) Then
                            results.Add(semanticModel.GetDeclaredSymbol(cast.Identifier.Parent, cancellationToken))
                        End If
                    End If
                End If
            Next

            Return results.ToImmutableAndFree()
        End Function

        Protected Overrides Function GetFormattingRules(document As Document) As ImmutableArray(Of AbstractFormattingRule)
            Dim coreRules = Formatter.GetDefaultFormattingRules(document)
            Dim result = New FixedSizeArrayBuilder(Of AbstractFormattingRule)(1 + coreRules.Length)
            result.Add(New ChangeSignatureFormattingRule())
            result.AddRange(coreRules)
            Return result.MoveToImmutable()
        End Function

        Protected Overrides Function TransferLeadingWhitespaceTrivia(Of T As SyntaxNode)(newArgument As T, oldArgument As SyntaxNode) As T
            Return newArgument
        End Function

        Protected Overrides ReadOnly Property Generator As SyntaxGenerator
            Get
                Return VisualBasicSyntaxGenerator.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides Function CreateExplicitParamsArrayFromIndividualArguments(Of TArgumentSyntax As SyntaxNode)(newArguments As SeparatedSyntaxList(Of TArgumentSyntax), indexInExistingList As Integer, parameterSymbol As IParameterSymbol) As TArgumentSyntax
            ' A params array cannot be introduced due to the addition of an omitted 
            ' argument in VB because you cannot have a named argument to a params array.
            Throw New InvalidOperationException()
        End Function

        Protected Overrides Function AddNameToArgument(Of TArgumentSyntax As SyntaxNode)(newArgument As TArgumentSyntax, name As String) As TArgumentSyntax
            Dim simpleArgument = TryCast(newArgument, SimpleArgumentSyntax)
            If simpleArgument IsNot Nothing Then
                Return CType(CType(simpleArgument.WithNameColonEquals(NameColonEquals(IdentifierName(name))), SyntaxNode), TArgumentSyntax)
            End If

            Dim omittedArgument = TryCast(newArgument, OmittedArgumentSyntax)
            If omittedArgument IsNot Nothing Then
                Return CType(CType(omittedArgument, SyntaxNode), TArgumentSyntax)
            End If

            Throw ExceptionUtilities.UnexpectedValue(newArgument.Kind())
        End Function

        Protected Overrides Function SupportsOptionalAndParamsArrayParametersSimultaneously() As Boolean
            Return False
        End Function

        Protected Overrides Function CommaTokenWithElasticSpace() As SyntaxToken
            Return Token(SyntaxKind.CommaToken).WithTrailingTrivia(ElasticSpace)
        End Function

        Protected Overrides Function TryGetRecordPrimaryConstructor(typeSymbol As INamedTypeSymbol, ByRef primaryConstructor As IMethodSymbol) As Boolean
            Return False
        End Function

        Protected Overrides Function GetParameters(declarationSymbol As ISymbol) As ImmutableArray(Of IParameterSymbol)
            Return declarationSymbol.GetParameters()
        End Function
    End Class
End Namespace
