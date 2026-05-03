' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateVariable
    <ExportLanguageService(GetType(IGenerateVariableService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateVariableService
        Inherits AbstractGenerateVariableService(Of VisualBasicGenerateVariableService, SimpleNameSyntax, ExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsExplicitInterfaceGeneration(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean
            Return TypeOf node Is QualifiedNameSyntax
        End Function

        Protected Overrides Function IsIdentifierNameGeneration(node As Microsoft.CodeAnalysis.SyntaxNode) As Boolean
            Return TypeOf node Is IdentifierNameSyntax
        End Function

        Protected Overrides Function TryInitializeExplicitInterfaceState(
                document As SemanticDocument, node As SyntaxNode, cancellationToken As CancellationToken,
                ByRef identifierToken As SyntaxToken, ByRef propertySymbol As IPropertySymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
            identifierToken = qualifiedName.Right.Identifier

            If qualifiedName.IsParentKind(SyntaxKind.ImplementsClause) Then

                Dim implementsClause = DirectCast(qualifiedName.Parent, ImplementsClauseSyntax)
                If implementsClause.IsParentKind(SyntaxKind.PropertyStatement) OrElse
                   implementsClause.IsParentKind(SyntaxKind.PropertyBlock) Then

                    Dim propertyBlock = TryCast(implementsClause.Parent, PropertyBlockSyntax)
                    Dim propertyNode = If(implementsClause.IsParentKind(SyntaxKind.PropertyStatement),
                                          DirectCast(implementsClause.Parent, PropertyStatementSyntax),
                                          propertyBlock.PropertyStatement)

                    Dim semanticModel = document.SemanticModel

                    propertySymbol = DirectCast(semanticModel.GetDeclaredSymbol(propertyNode, cancellationToken), IPropertySymbol)
                    If propertySymbol IsNot Nothing AndAlso Not propertySymbol.ExplicitInterfaceImplementations.Any() Then

                        Dim info = semanticModel.GetTypeInfo(qualifiedName.Left, cancellationToken)
                        typeToGenerateIn = TryCast(info.Type, INamedTypeSymbol)
                        Return typeToGenerateIn IsNot Nothing
                    End If
                End If
            End If

            identifierToken = Nothing
            propertySymbol = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function TryInitializeIdentifierNameState(
                document As SemanticDocument, identifierName As SimpleNameSyntax,
                cancellationToken As CancellationToken,
                ByRef identifierToken As SyntaxToken,
                ByRef simpleNameOrMemberAccessExpression As ExpressionSyntax,
                ByRef isInExecutableBlock As Boolean,
                ByRef isConditionalAccessExpression As Boolean) As Boolean
            identifierToken = identifierName.Identifier

            Dim memberAccess = TryCast(identifierName.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                If memberAccess.Kind = SyntaxKind.DictionaryAccessExpression Then
                    Return False
                End If
            End If

            Dim conditionalMemberAccess = TryCast(identifierName.Parent.Parent, ConditionalAccessExpressionSyntax)

            If memberAccess?.Name Is identifierName Then
                simpleNameOrMemberAccessExpression = DirectCast(memberAccess, ExpressionSyntax)
            ElseIf TryCast(conditionalMemberAccess?.WhenNotNull, MemberAccessExpressionSyntax)?.Name Is identifierName Then
                simpleNameOrMemberAccessExpression = conditionalMemberAccess
            Else
                simpleNameOrMemberAccessExpression = identifierName
            End If

            Dim semanticModel = document.SemanticModel
            If Not IsLegal(semanticModel, simpleNameOrMemberAccessExpression, cancellationToken) AndAlso
               Not simpleNameOrMemberAccessExpression.Parent.IsKind(SyntaxKind.NameOfExpression, SyntaxKind.NamedFieldInitializer) Then
                identifierToken = Nothing
                isConditionalAccessExpression = Nothing
                simpleNameOrMemberAccessExpression = Nothing
                Return False
            End If

            Dim block = identifierToken.Parent.GetContainingMultiLineExecutableBlocks().FirstOrDefault()
            isInExecutableBlock = block IsNot Nothing AndAlso Not block.OverlapsHiddenPosition(cancellationToken)
            isConditionalAccessExpression = conditionalMemberAccess IsNot Nothing
            Return True
        End Function

        Private Shared Function IsLegal(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            Dim tree = semanticModel.SyntaxTree
            Dim position = expression.SpanStart

            If Not tree.IsExpressionContext(position, cancellationToken) AndAlso
               Not tree.IsSingleLineStatementContext(position, cancellationToken) Then
                Return False
            End If

            Return expression.CanReplaceWithLValue(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function TryConvertToLocalDeclaration(type As ITypeSymbol, identifierToken As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken, ByRef newRoot As SyntaxNode) As Boolean
            Return False
        End Function
    End Class
End Namespace
