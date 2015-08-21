' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateMethod
    <ExportLanguageService(GetType(IGenerateParameterizedMemberService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateMethodService
        Inherits AbstractGenerateMethodService(Of VisualBasicGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax)

        Protected Overrides Function IsExplicitInterfaceGeneration(node As SyntaxNode) As Boolean
            Return TypeOf node Is QualifiedNameSyntax
        End Function

        Protected Overrides Function IsSimpleNameGeneration(node As SyntaxNode) As Boolean
            Return TypeOf node Is SimpleNameSyntax
        End Function

        Protected Overrides Function AreSpecialOptionsActive(semanticModel As SemanticModel) As Boolean
            Return VisualBasicCommonGenerationServiceMethods.AreSpecialOptionsActive(semanticModel)
        End Function

        Protected Overrides Function IsValidSymbol(symbol As ISymbol, semanticModel As SemanticModel) As Boolean
            Return VisualBasicCommonGenerationServiceMethods.IsValidSymbol(symbol, semanticModel)
        End Function

        Protected Overrides Function TryInitializeExplicitInterfaceState(
                document As SemanticDocument,
                node As SyntaxNode,
                cancellationToken As CancellationToken,
                ByRef identifierToken As SyntaxToken,
                ByRef methodSymbol As IMethodSymbol,
                ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
            identifierToken = qualifiedName.Right.Identifier

            If qualifiedName.IsParentKind(SyntaxKind.ImplementsClause) Then
                Dim implementsClause = DirectCast(qualifiedName.Parent, ImplementsClauseSyntax)

                If implementsClause.IsParentKind(SyntaxKind.SubStatement) OrElse
                    implementsClause.IsParentKind(SyntaxKind.FunctionStatement) Then

                    Dim methodStatement = DirectCast(implementsClause.Parent, MethodStatementSyntax)
                    Dim semanticModel = document.SemanticModel

                    methodSymbol = DirectCast(semanticModel.GetDeclaredSymbol(methodStatement, cancellationToken), IMethodSymbol)
                    If methodSymbol IsNot Nothing AndAlso Not methodSymbol.ExplicitInterfaceImplementations.Any() Then
                        Dim semanticInfo = semanticModel.GetTypeInfo(qualifiedName.Left, cancellationToken)
                        typeToGenerateIn = TryCast(semanticInfo.Type, INamedTypeSymbol)
                        Return typeToGenerateIn IsNot Nothing
                    End If
                End If
            End If

            identifierToken = Nothing
            methodSymbol = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function TryInitializeSimpleNameState(
                document As SemanticDocument,
                simpleName As SimpleNameSyntax,
                cancellationToken As CancellationToken,
                ByRef identifierToken As SyntaxToken,
                ByRef simpleNameOrMemberAccessExpression As ExpressionSyntax,
                ByRef invocationExpressionOpt As InvocationExpressionSyntax,
                ByRef isInConditionalAccessExpression As Boolean) As Boolean

            identifierToken = simpleName.Identifier
            Dim memberAccess = TryCast(simpleName?.Parent, MemberAccessExpressionSyntax)
            Dim conditionalMemberAccessInvocationExpression = TryCast(simpleName?.Parent?.Parent?.Parent, ConditionalAccessExpressionSyntax)
            Dim conditionalMemberAccessSimpleMemberAccess = TryCast(simpleName?.Parent?.Parent, ConditionalAccessExpressionSyntax)
            If memberAccess?.Name Is simpleName Then
                simpleNameOrMemberAccessExpression = memberAccess
            ElseIf TryCast(TryCast(conditionalMemberAccessInvocationExpression?.WhenNotNull, InvocationExpressionSyntax)?.Expression, MemberAccessExpressionSyntax)?.Name Is simpleName Then
                simpleNameOrMemberAccessExpression = conditionalMemberAccessInvocationExpression
            ElseIf TryCast(conditionalMemberAccessSimpleMemberAccess?.WhenNotNull, MemberAccessExpressionSyntax)?.Name Is simpleName Then
                simpleNameOrMemberAccessExpression = conditionalMemberAccessSimpleMemberAccess
            Else
                simpleNameOrMemberAccessExpression = simpleName
            End If

            If memberAccess Is Nothing OrElse memberAccess.Name Is simpleName Then

                ' VB is ambiguous.  Something that looks like a method call might
                ' actually just be an array access.  Check for that here.
                Dim semanticModel = document.SemanticModel
                Dim nameSemanticInfo = semanticModel.GetTypeInfo(simpleNameOrMemberAccessExpression, cancellationToken)

                If TypeOf nameSemanticInfo.Type IsNot IArrayTypeSymbol Then
                    ' Don't offer generate method if it's a call to another constructor inside a
                    ' constructor.
                    If Not memberAccess.IsConstructorInitializer() Then
                        If cancellationToken.IsCancellationRequested Then
                            isInConditionalAccessExpression = False
                            Return False
                        End If

                        isInConditionalAccessExpression = conditionalMemberAccessInvocationExpression IsNot Nothing Or conditionalMemberAccessSimpleMemberAccess IsNot Nothing

                        If simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) Then
                            invocationExpressionOpt = DirectCast(simpleNameOrMemberAccessExpression.Parent, InvocationExpressionSyntax)
                            Return invocationExpressionOpt.ArgumentList Is Nothing OrElse
                               Not invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing
                        ElseIf TryCast(TryCast(TryCast(simpleNameOrMemberAccessExpression, ConditionalAccessExpressionSyntax)?.WhenNotNull, InvocationExpressionSyntax)?.Expression, MemberAccessExpressionSyntax)?.Name Is simpleName
                            invocationExpressionOpt = DirectCast(DirectCast(simpleNameOrMemberAccessExpression, ConditionalAccessExpressionSyntax).WhenNotNull, InvocationExpressionSyntax)
                            Return invocationExpressionOpt.ArgumentList Is Nothing OrElse
                               Not invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing
                        ElseIf TryCast(conditionalMemberAccessSimpleMemberAccess?.WhenNotNull, MemberAccessExpressionSyntax)?.Name Is simpleName AndAlso
                               IsLegal(semanticModel, simpleNameOrMemberAccessExpression, cancellationToken) Then
                            Return True
                        ElseIf simpleNameOrMemberAccessExpression?.Parent?.IsKind(SyntaxKind.AddressOfExpression, SyntaxKind.NameOfExpression) Then
                            Return True
                        ElseIf IsLegal(semanticModel, simpleNameOrMemberAccessExpression, cancellationToken) Then
                            simpleNameOrMemberAccessExpression =
                                If(memberAccess IsNot Nothing AndAlso memberAccess.Name Is simpleName,
                                   DirectCast(memberAccess, ExpressionSyntax),
                                   simpleName)
                            Return True
                        End If
                    End If
                End If
            End If

            identifierToken = Nothing
            simpleNameOrMemberAccessExpression = Nothing
            invocationExpressionOpt = Nothing
            isInConditionalAccessExpression = False
            Return False
        End Function

        Private Function IsLegal(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            Dim tree = semanticModel.SyntaxTree
            Dim position = expression.SpanStart

            If Not tree.IsExpressionContext(position, cancellationToken) AndAlso
               Not tree.IsSingleLineStatementContext(position, cancellationToken) Then
                Return False
            End If

            Return expression.CanReplaceWithLValue(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function CreateInvocationMethodInfo(document As SemanticDocument, state As AbstractGenerateParameterizedMemberService(Of VisualBasicGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax).State) As AbstractInvocationInfo
            Return New VisualBasicGenerateParameterizedMemberService(Of VisualBasicGenerateMethodService).InvocationExpressionInfo(document, state)
        End Function

        Protected Overrides Function CanGenerateMethodForSimpleNameOrMemberAccessExpression(typeInferenceService As ITypeInferenceService, semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As ITypeSymbol
            Return typeInferenceService.InferType(semanticModel, expression, True, cancellationToken)
        End Function
    End Class
End Namespace
