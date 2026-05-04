' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor
    <ExportLanguageService(GetType(IGenerateConstructorService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateConstructorService
        Inherits AbstractGenerateConstructorService(Of VisualBasicGenerateConstructorService, ExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsImplicitObjectCreation(document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return False
        End Function

        Protected Overrides Function TryInitializeImplicitObjectCreation(document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken, ByRef token As SyntaxToken, ByRef arguments As ImmutableArray(Of Argument(Of ExpressionSyntax)), ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            token = Nothing
            arguments = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function ContainingTypesOrSelfHasUnsafeKeyword(containingType As INamedTypeSymbol) As Boolean
            Return False
        End Function

        Protected Overrides Function GenerateNameForExpression(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As String
            Return semanticModel.GenerateNameForExpression(expression, capitalize:=False, cancellationToken)
        End Function

        Protected Overrides Function GetArgumentType(
                semanticModel As SemanticModel,
                argument As Argument(Of ExpressionSyntax),
                cancellationToken As CancellationToken) As ITypeSymbol
            Return argument.Expression.DetermineType(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function IsConstructorInitializerGeneration(document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return TypeOf node Is SimpleNameSyntax AndAlso
                TryCast(node.Parent, MemberAccessExpressionSyntax).IsConstructorInitializer() AndAlso
                node.Parent.Parent.Kind = SyntaxKind.InvocationExpression
        End Function

        Protected Overrides Function TryInitializeConstructorInitializerGeneration(
                document As SemanticDocument,
                node As SyntaxNode,
                cancellationToken As CancellationToken,
                ByRef token As SyntaxToken,
                ByRef arguments As ImmutableArray(Of Argument(Of ExpressionSyntax)),
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim simpleName = DirectCast(node, SimpleNameSyntax)
            Dim memberAccess = DirectCast(simpleName.Parent, MemberAccessExpressionSyntax)
            Dim invocation = DirectCast(memberAccess.Parent, InvocationExpressionSyntax)
            If invocation.ArgumentList IsNot Nothing AndAlso Not invocation.ArgumentList.CloseParenToken.IsMissing Then
                Dim semanticModel = document.SemanticModel
                Dim containingType = semanticModel.GetEnclosingNamedType(simpleName.SpanStart, cancellationToken)

                If containingType IsNot Nothing Then
                    token = simpleName.Identifier
                    arguments = GetArguments(invocation.ArgumentList.Arguments)
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
                ByRef arguments As ImmutableArray(Of Argument(Of ExpressionSyntax)),
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean

            Dim simpleName = DirectCast(node, SimpleNameSyntax)
            Dim name = If(simpleName.IsRightSideOfQualifiedName(), DirectCast(simpleName.Parent, NameSyntax), simpleName)

            If TypeOf name.Parent Is ObjectCreationExpressionSyntax Then
                Dim objectCreationExpression = DirectCast(name.Parent, ObjectCreationExpressionSyntax)

                If objectCreationExpression.ArgumentList IsNot Nothing AndAlso
                   Not objectCreationExpression.ArgumentList.CloseParenToken.IsMissing Then

                    Dim semanticModel = document.SemanticModel

                    token = simpleName.Identifier
                    arguments = GetArguments(objectCreationExpression.ArgumentList.Arguments)

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
                ByRef arguments As ImmutableArray(Of Argument(Of ExpressionSyntax)),
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
                        arguments = GetArguments(attribute.ArgumentList.Arguments)
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

        Private Shared Function GetArguments(arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As ImmutableArray(Of Argument(Of ExpressionSyntax))
            Return arguments.SelectAsArray(AddressOf InitializeParameterHelpers.GetArgument)
        End Function

        Protected Overrides Function IsConversionImplicit(compilation As Compilation, sourceType As ITypeSymbol, targetType As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(sourceType, targetType).IsWidening
        End Function

        Protected Overrides Function GetCurrentConstructor(semanticModel As SemanticModel, token As SyntaxToken, cancellationToken As CancellationToken) As IMethodSymbol
            Dim subNewStatement = token.GetAncestor(Of ConstructorBlockSyntax)()?.SubNewStatement
            Return If(subNewStatement IsNot Nothing, semanticModel.GetDeclaredSymbol(subNewStatement, cancellationToken), Nothing)
        End Function

        Protected Overrides Function GetDelegatedConstructor(semanticModel As SemanticModel, constructor As IMethodSymbol, cancellationToken As CancellationToken) As IMethodSymbol
            Dim constructorStatements = constructor.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken).Parent.GetStatements()
            If (constructorStatements.IsEmpty()) Then
                Return Nothing
            End If

            Dim constructorInitializerSyntax = constructorStatements(0)
            Dim expressionStatement = TryCast(constructorInitializerSyntax, ExpressionStatementSyntax)
            If (expressionStatement IsNot Nothing AndAlso expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression)) Then
                Dim methodSymbol = TryCast(semanticModel.GetSymbolInfo(expressionStatement.Expression, cancellationToken).Symbol, IMethodSymbol)
                Return If(methodSymbol IsNot Nothing AndAlso methodSymbol.MethodKind = MethodKind.Constructor, methodSymbol, Nothing)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
