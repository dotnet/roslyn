' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateConstructor
    <ExportLanguageService(GetType(IGenerateConstructorService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateConstructorService
        Inherits AbstractGenerateConstructorService(Of VisualBasicGenerateConstructorService, ArgumentSyntax, AttributeSyntax)

        Protected Overrides Function GenerateNameForArgument(semanticModel As SemanticModel, argument As ArgumentSyntax) As String
            Return semanticModel.GenerateNameForArgument(argument)
        End Function

        Protected Overrides Function GenerateParameterNames(semanticModel As SemanticModel, arguments As IEnumerable(Of ArgumentSyntax), Optional reservedNames As IList(Of String) = Nothing) As IList(Of String)
            Return semanticModel.GenerateParameterNames(arguments?.ToList(), reservedNames)
        End Function

        Protected Overrides Function GetArgumentType(semanticModel As SemanticModel, argument As ArgumentSyntax, cancellationToken As CancellationToken) As Microsoft.CodeAnalysis.ITypeSymbol
            Return argument.DetermineType(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function GetRefKind(argument As ArgumentSyntax) As RefKind
            ' TODO(cyrusn): If the argument is a parameter, then consider copying over its refkind.
            Return RefKind.None
        End Function

        Protected Overrides Function IsNamedArgument(argument As ArgumentSyntax) As Boolean
            Return argument.IsNamed
        End Function

        Protected Overrides Function IsConstructorInitializerGeneration(document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return TypeOf node Is SimpleNameSyntax AndAlso
                TryCast(node.Parent, MemberAccessExpressionSyntax).IsConstructorInitializer() AndAlso
                node.Parent.Parent.Kind = SyntaxKind.InvocationExpression
        End Function

        Protected Overrides Function TryInitializeConstructorInitializerGeneration(
                document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken,
                ByRef token As SyntaxToken, ByRef arguments As IList(Of ArgumentSyntax), ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim simpleName = DirectCast(node, SimpleNameSyntax)
            Dim memberAccess = DirectCast(simpleName.Parent, MemberAccessExpressionSyntax)
            Dim invocation = DirectCast(memberAccess.Parent, InvocationExpressionSyntax)
            If invocation.ArgumentList IsNot Nothing AndAlso Not invocation.ArgumentList.CloseParenToken.IsMissing Then
                Dim semanticModel = document.SemanticModel
                Dim containingType = semanticModel.GetEnclosingNamedType(simpleName.SpanStart, cancellationToken)

                If containingType IsNot Nothing Then
                    token = simpleName.Identifier
                    arguments = invocation.ArgumentList.Arguments.ToList()
                    typeToGenerateIn = If(memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression),
                                          containingType.BaseType,
                                          containingType)

                    Return typeToGenerateIn IsNot Nothing
                End If
            End If

            token = Nothing
            arguments = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function IsSimpleNameGeneration(document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return TypeOf node Is SimpleNameSyntax
        End Function

        Protected Overrides Function TryInitializeSimpleNameGenerationState(
                document As SemanticDocument,
                node As SyntaxNode,
                cancellationToken As CancellationToken,
                ByRef token As SyntaxToken,
                ByRef arguments As IList(Of ArgumentSyntax),
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean

            Dim simpleName = DirectCast(node, SimpleNameSyntax)
            Dim name = If(simpleName.IsRightSideOfQualifiedName(), DirectCast(simpleName.Parent, NameSyntax), simpleName)

            If TypeOf name.Parent Is ObjectCreationExpressionSyntax Then
                Dim objectCreationExpression = DirectCast(name.Parent, ObjectCreationExpressionSyntax)

                If objectCreationExpression.ArgumentList IsNot Nothing AndAlso
                   Not objectCreationExpression.ArgumentList.CloseParenToken.IsMissing Then

                    Dim semanticModel = document.SemanticModel

                    token = simpleName.Identifier
                    arguments = objectCreationExpression.ArgumentList.Arguments.ToList()

                    Dim symbolInfo = semanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken)
                    typeToGenerateIn = TryCast(symbolInfo.GetAnySymbol(), INamedTypeSymbol)

                    Return typeToGenerateIn IsNot Nothing
                End If
            End If

            token = Nothing
            arguments = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function TryInitializeSimpleAttributeNameGenerationState(
                document As SemanticDocument,
                node As SyntaxNode,
                cancellationToken As CancellationToken,
                ByRef token As SyntaxToken,
                ByRef arguments As IList(Of ArgumentSyntax),
                ByRef attributeArguments As IList(Of AttributeSyntax),
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean

            Dim simpleName = DirectCast(node, SimpleNameSyntax)
            Dim name = If(simpleName.IsRightSideOfQualifiedName(), DirectCast(simpleName.Parent, NameSyntax), simpleName)

            If TypeOf name.Parent Is AttributeSyntax Then
                Dim attribute = DirectCast(name.Parent, AttributeSyntax)

                If attribute.ArgumentList IsNot Nothing AndAlso
                   Not attribute.ArgumentList.CloseParenToken.IsMissing Then

                    Dim symbolInfo = document.SemanticModel.GetSymbolInfo(attribute, cancellationToken)
                    If symbolInfo.CandidateReason = CandidateReason.OverloadResolutionFailure AndAlso Not symbolInfo.CandidateSymbols.IsEmpty Then
                        token = simpleName.Identifier
                        arguments = attribute.ArgumentList.Arguments.ToList()
                        attributeArguments = Nothing
                        typeToGenerateIn = TryCast(symbolInfo.CandidateSymbols.FirstOrDefault().ContainingSymbol, INamedTypeSymbol)

                        Return typeToGenerateIn IsNot Nothing
                    End If
                End If
            End If

            token = Nothing
            arguments = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function IsConversionImplicit(compilation As Compilation, sourceType As ITypeSymbol, targetType As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(sourceType, targetType).IsWidening
        End Function

        Protected Overrides Function IsClassDeclarationGeneration(document As SemanticDocument,
                                                                  node As SyntaxNode,
                                                                  cancellationToken As CancellationToken) As Boolean
            Return TypeOf node Is ClassBlockSyntax
        End Function

        Protected Overrides Function TryInitializeClassDeclarationGenerationState(
                document As SemanticDocument,
                classDeclaration As SyntaxNode,
                cancellationToken As CancellationToken,
                ByRef token As SyntaxToken,
                ByRef delegatedConstructor As IMethodSymbol,
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim semanticModel = document.SemanticModel
            Dim classBlock = DirectCast(classDeclaration, ClassBlockSyntax)
            Dim classSymbol = semanticModel.GetDeclaredSymbol(classBlock.BlockStatement, cancellationToken)

            Dim baseType = classSymbol.BaseType
            Dim constructor = baseType.Constructors.FirstOrDefault(Function(c) IsSymbolAccessible(c, document))
            If constructor Is Nothing Then
                Return False
            End If

            typeToGenerateIn = classSymbol
            delegatedConstructor = constructor
            token = classBlock.BlockStatement.Identifier
            Return True
        End Function

        Private Shared ReadOnly s_annotation As SyntaxAnnotation = New SyntaxAnnotation

        Friend Overrides Function GetDelegatingConstructor(state As State,
                                                           document As SemanticDocument,
                                                           argumentCount As Integer,
                                                           namedType As INamedTypeSymbol,
                                                           candidates As ISet(Of IMethodSymbol),
                                                           cancellationToken As CancellationToken) As IMethodSymbol
            Dim oldToken = state.Token
            Dim tokenKind = oldToken.Kind()
            Dim simpleName = DirectCast(oldToken.Parent, SimpleNameSyntax)

            If state.IsConstructorInitializerGeneration Then
                Dim memberAccess = DirectCast(simpleName.Parent, MemberAccessExpressionSyntax)
                Dim invocation = DirectCast(memberAccess.Parent, InvocationExpressionSyntax)
                Dim invocationStatement = memberAccess.FirstAncestorOrSelf(Of ExecutableStatementSyntax)

                Dim meOrMybaseExpression As ExpressionSyntax
                If Not memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) AndAlso state.TypeToGenerateIn.Equals(namedType) Then
                    meOrMybaseExpression = SyntaxFactory.MeExpression
                Else
                    meOrMybaseExpression = SyntaxFactory.MyBaseExpression
                End If

                Dim newInvocation = invocation.ReplaceNode(memberAccess.Expression, meOrMybaseExpression).WithAdditionalAnnotations(s_annotation)
                Dim newInvocationStatement = invocationStatement.ReplaceNode(invocation, newInvocation)
                newInvocation = DirectCast(newInvocationStatement.GetAnnotatedNodes(s_annotation).Single(), InvocationExpressionSyntax)

                Dim oldArgumentList = newInvocation.ArgumentList
                Dim newArgumentList = GetNewArgumentList(oldArgumentList, argumentCount)
                If (newArgumentList IsNot oldArgumentList) Then
                    newInvocationStatement = newInvocationStatement.ReplaceNode(oldArgumentList, newArgumentList)
                    newInvocation = DirectCast(newInvocationStatement.GetAnnotatedNodes(s_annotation).Single(), InvocationExpressionSyntax)
                End If

                Dim speculativeModel As SemanticModel = Nothing
                If document.SemanticModel.TryGetSpeculativeSemanticModel(invocationStatement.SpanStart, newInvocationStatement, speculativeModel) Then
                    Dim symbolInfo = speculativeModel.GetSymbolInfo(newInvocation, cancellationToken)
                    Return GenerateConstructorHelpers.GetDelegatingConstructor(
                        document, symbolInfo, candidates, namedType, state.ParameterTypes)
                End If
            Else
                Dim oldNode = oldToken.Parent _
                    .AncestorsAndSelf(ascendOutOfTrivia:=False) _
                    .Where(Function(node) SpeculationAnalyzer.CanSpeculateOnNode(node)) _
                    .LastOrDefault()

                Dim typeNameToReplace = DirectCast(oldToken.Parent, TypeSyntax)
                Dim newTypeName As TypeSyntax
                If namedType IsNot state.TypeToGenerateIn Then
                    While True
                        Dim parentType = TryCast(typeNameToReplace.Parent, TypeSyntax)
                        If parentType Is Nothing Then
                            Exit While
                        End If

                        typeNameToReplace = parentType
                    End While

                    newTypeName = namedType.GenerateTypeSyntax().WithAdditionalAnnotations(s_annotation)
                Else
                    newTypeName = typeNameToReplace.WithAdditionalAnnotations(s_annotation)
                End If

                Dim newNode = oldNode.ReplaceNode(typeNameToReplace, newTypeName)
                newTypeName = DirectCast(newNode.GetAnnotatedNodes(s_annotation).[Single](), TypeSyntax)

                Dim oldArgumentList = DirectCast(newTypeName.Parent.ChildNodes().FirstOrDefault(Function(n) TypeOf n Is ArgumentListSyntax), ArgumentListSyntax)
                If oldArgumentList IsNot Nothing Then
                    Dim newArgumentList = GetNewArgumentList(oldArgumentList, argumentCount)
                    If newArgumentList IsNot oldArgumentList Then
                        newNode = newNode.ReplaceNode(oldArgumentList, newArgumentList)
                        newTypeName = DirectCast(newNode.GetAnnotatedNodes(s_annotation).Single(), TypeSyntax)
                    End If
                End If

                Dim speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(oldNode, newNode, document.SemanticModel)
                If speculativeModel IsNot Nothing Then
                    Dim symbolInfo = speculativeModel.GetSymbolInfo(newTypeName.Parent, cancellationToken)
                    Return GenerateConstructorHelpers.GetDelegatingConstructor(
                        document, symbolInfo, candidates, namedType, state.ParameterTypes)
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function GetNewArgumentList(oldArgumentList As ArgumentListSyntax, argumentCount As Integer) As ArgumentListSyntax
            If oldArgumentList.IsMissing OrElse oldArgumentList.Arguments.Count = argumentCount Then
                Return oldArgumentList
            End If

            Dim newArguments = oldArgumentList.Arguments.Take(argumentCount)
            Return SyntaxFactory.ArgumentList(New SeparatedSyntaxList(Of ArgumentSyntax)().AddRange(newArguments))
        End Function
    End Class
End Namespace
