' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Compilers
Imports Microsoft.CodeAnalysis.Compilers.Common
Imports Microsoft.CodeAnalysis.Compilers.VisualBasic
Imports Microsoft.CodeAnalysis.Services.Editor.Implementation.GenerateMember.GenerateFieldOrProperty
Imports Microsoft.CodeAnalysis.Services.Editor.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.Services.Shared.CodeGeneration
Imports Microsoft.CodeAnalysis.Services.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.Services.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Services.Editor.VisualBasic.GenerateMember.GenerateFieldOrProperty
    <ExportLanguageService(GetType(IGenerateFieldOrPropertyService), LanguageNames.VisualBasic)>
    Partial Friend Class VisualBasicGenerateFieldOrPropertyService
        Inherits AbstractGenerateFieldOrPropertyService(Of VisualBasicGenerateFieldOrPropertyService, SimpleNameSyntax, ExpressionSyntax)

        <ImportingConstructor()>
        Public Sub New(
            languageServiceProviderFactory As ILanguageServiceProviderFactory,
            codeDefinitionFactory As ICodeDefinitionFactory)
            MyBase.New(languageServiceProviderFactory, codeDefinitionFactory)
        End Sub

        Protected Overrides Function IsExplicitInterfaceGeneration(node As Microsoft.CodeAnalysis.Common.CommonSyntaxNode) As Boolean
            Return TypeOf node Is QualifiedNameSyntax
        End Function

        Protected Overrides Function IsIdentifierNameGeneration(node As Microsoft.CodeAnalysis.Common.CommonSyntaxNode) As Boolean
            Return TypeOf node Is IdentifierNameSyntax
        End Function

        Protected Overrides Function TryInitializeExplicitInterfaceState(
                document As IDocument, node As CommonSyntaxNode, cancellationToken As CancellationToken,
                ByRef identifierToken As CommonSyntaxToken, ByRef propertySymbol As IPropertySymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
            identifierToken = qualifiedName.Right.Identifier

            If qualifiedName.IsParentKind(SyntaxKind.ImplementsClause) Then

                Dim implementsClause = DirectCast(qualifiedName.Parent, ImplementsClauseSyntax)
                If implementsClause.IsParentKind(SyntaxKind.PropertyStatement) OrElse
                   implementsClause.IsParentKind(SyntaxKind.PropertyBlock) Then

                    Dim propertyBlock = TryCast(implementsClause.Parent, PropertyBlockSyntax)
                    Dim propertyNode = If(implementsClause.IsParentKind(SyntaxKind.PropertyStatement),
                                          DirectCast(implementsClause.Parent, PropertyStatementSyntax),
                                          propertyBlock.Begin)

                    Dim semanticModel = document.GetSemanticModel(cancellationToken)

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
                document As IDocument, identifierName As SimpleNameSyntax, cancellationToken As CancellationToken,
                ByRef identifierToken As CommonSyntaxToken, ByRef simpleNameOrMemberAccessExpression As ExpressionSyntax) As Boolean
            identifierToken = identifierName.Identifier

            Dim memberAccess = TryCast(identifierName.Parent, MemberAccessExpressionSyntax)
            simpleNameOrMemberAccessExpression =
                If(memberAccess IsNot Nothing AndAlso memberAccess.Name Is identifierName,
                   DirectCast(memberAccess, ExpressionSyntax),
                   identifierName)

            If Not simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) AndAlso
               Not simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.ObjectCreationExpression) AndAlso
               Not simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.AddressOfExpression) Then
                Dim syntaxTree = document.GetVisualBasicSyntaxTree(cancellationToken)

                If syntaxTree.IsExpressionContext(simpleNameOrMemberAccessExpression.Span.Start) OrElse
                   syntaxTree.IsSingleLineStatementContext(simpleNameOrMemberAccessExpression.Span.Start) Then
                    Return True
                End If
            End If

            identifierToken = Nothing
            simpleNameOrMemberAccessExpression = Nothing
            Return False
        End Function
    End Class
End Namespace
