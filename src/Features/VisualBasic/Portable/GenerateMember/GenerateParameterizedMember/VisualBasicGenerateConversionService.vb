﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateMethod
    <ExportLanguageService(GetType(IGenerateConversionService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateConversionService
        Inherits AbstractGenerateConversionService(Of VisualBasicGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax)

        Protected Overrides Function AreSpecialOptionsActive(semanticModel As SemanticModel) As Boolean
            Return VisualBasicCommonGenerationServiceMethods.AreSpecialOptionsActive(semanticModel)
        End Function

        Protected Overrides Function CreateInvocationMethodInfo(document As SemanticDocument, abstractState As AbstractGenerateParameterizedMemberService(Of VisualBasicGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax).State) As AbstractInvocationInfo
            Return New VisualBasicGenerateParameterizedMemberService(Of VisualBasicGenerateConversionService).InvocationExpressionInfo(document, abstractState)
        End Function

        Protected Overrides Function IsExplicitConversionGeneration(node As SyntaxNode) As Boolean
            Return node.AncestorsAndSelf.Where(AddressOf IsCastExpression).Where(Function(n) n.Span.Contains(node.Span)).Any
        End Function

        Protected Overrides Function IsImplicitConversionGeneration(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExpressionSyntax AndAlso
                Not IsExplicitConversionGeneration(node) AndAlso
                Not IsInMemberAccessExpression(node) AndAlso
                Not IsInImplementsClause(node)
        End Function

        Private Function IsInImplementsClause(node As SyntaxNode) As Boolean
            Return node.AncestorsAndSelf.Where(Function(n) n.IsKind(SyntaxKind.ImplementsClause)).Where(Function(n) n.Span.Contains(node.Span)).Any
        End Function

        Private Function IsInMemberAccessExpression(node As SyntaxNode) As Boolean
            Return node.AncestorsAndSelf.Where(Function(n) n.IsKind(SyntaxKind.SimpleMemberAccessExpression)).Where(Function(n) n.Span.Contains(node.Span)).Any
        End Function

        Protected Overrides Function IsValidSymbol(symbol As ISymbol, semanticModel As SemanticModel) As Boolean
            Return VisualBasicCommonGenerationServiceMethods.IsValidSymbol(symbol, semanticModel)
        End Function

        Protected Overrides Function TryInitializeExplicitConversionState(document As SemanticDocument, expression As SyntaxNode, classInterfaceModuleStructTypes As ISet(Of TypeKind), cancellationToken As CancellationToken, ByRef identifierToken As SyntaxToken, ByRef methodSymbol As IMethodSymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            If TryGetConversionMethodAndTypeToGenerateIn(document, expression, classInterfaceModuleStructTypes, cancellationToken, methodSymbol, typeToGenerateIn) Then
                identifierToken = SyntaxFactory.Token(
                    SyntaxKind.NarrowingKeyword,
                    WellKnownMemberNames.ExplicitConversionName)
                Return True
            End If

            identifierToken = Nothing
            methodSymbol = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Protected Overrides Function TryInitializeImplicitConversionState(document As SemanticDocument, expression As SyntaxNode, classInterfaceModuleStructTypes As ISet(Of TypeKind), cancellationToken As CancellationToken, ByRef identifierToken As SyntaxToken, ByRef methodSymbol As IMethodSymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            If TryGetConversionMethodAndTypeToGenerateIn(document, expression, classInterfaceModuleStructTypes, cancellationToken, methodSymbol, typeToGenerateIn) Then
                identifierToken = SyntaxFactory.Token(
                    SyntaxKind.WideningKeyword,
                    WellKnownMemberNames.ImplicitConversionName)
                Return True
            End If

            identifierToken = Nothing
            methodSymbol = Nothing
            typeToGenerateIn = Nothing
            Return False
        End Function

        Private Function TryGetConversionMethodAndTypeToGenerateIn(document As SemanticDocument, expression As SyntaxNode, classInterfaceModuleStructTypes As ISet(Of TypeKind), cancellationToken As CancellationToken, ByRef methodSymbol As IMethodSymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            Dim castExpression = TryCast(expression.AncestorsAndSelf.Where(AddressOf IsCastExpression).Where(Function(n) n.Span.Contains(expression.Span)).FirstOrDefault, CastExpressionSyntax)
            If castExpression IsNot Nothing Then
                Return TryGetExplicitConversionMethodAndTypeToGenerateIn(
                    document,
                    castExpression,
                    classInterfaceModuleStructTypes,
                    cancellationToken,
                    methodSymbol,
                    typeToGenerateIn)
            End If

            expression = TryCast(expression.AncestorsAndSelf.Where(Function(n) TypeOf n Is ExpressionSyntax And n.Span.Contains(expression.Span)).FirstOrDefault, ExpressionSyntax)
            If expression IsNot Nothing Then
                Return TryGetImplicitConversionMethodAndTypeToGenerateIn(
                    document,
                    expression,
                    classInterfaceModuleStructTypes,
                    cancellationToken,
                    methodSymbol,
                    typeToGenerateIn)
            End If

            Return False
        End Function

        Private Shared Function IsCastExpression(node As SyntaxNode) As Boolean
            Return TypeOf node Is DirectCastExpressionSyntax OrElse TypeOf node Is CTypeExpressionSyntax OrElse TypeOf node Is TryCastExpressionSyntax
        End Function

        Private Function TryGetExplicitConversionMethodAndTypeToGenerateIn(document As SemanticDocument, castExpression As CastExpressionSyntax, classInterfaceModuleStructTypes As ISet(Of TypeKind), cancellationToken As CancellationToken, ByRef methodSymbol As IMethodSymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            methodSymbol = Nothing
            typeToGenerateIn = TryCast(document.SemanticModel.GetTypeInfo(castExpression.Type, cancellationToken).Type, INamedTypeSymbol)
            Dim parameterSymbol = TryCast(document.SemanticModel.GetTypeInfo(castExpression.Expression, cancellationToken).Type, INamedTypeSymbol)
            If typeToGenerateIn Is Nothing OrElse parameterSymbol Is Nothing OrElse typeToGenerateIn.IsErrorType OrElse parameterSymbol.IsErrorType Then
                Return False
            End If

            methodSymbol = GenerateMethodSymbol(typeToGenerateIn, parameterSymbol)
            If Not ValidateTypeToGenerateIn(document.Project.Solution, typeToGenerateIn, True, classInterfaceModuleStructTypes, cancellationToken) Then
                typeToGenerateIn = parameterSymbol
            End If
            Return True
        End Function

        Private Function TryGetImplicitConversionMethodAndTypeToGenerateIn(document As SemanticDocument, expression As SyntaxNode, classInterfaceModuleStructTypes As ISet(Of TypeKind), cancellationToken As CancellationToken, ByRef methodSymbol As IMethodSymbol, ByRef typeToGenerateIn As INamedTypeSymbol) As Boolean
            methodSymbol = Nothing
            typeToGenerateIn = TryCast(document.SemanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType, INamedTypeSymbol)
            Dim parameterSymbol = TryCast(document.SemanticModel.GetTypeInfo(expression, cancellationToken).Type, INamedTypeSymbol)
            If typeToGenerateIn Is Nothing OrElse parameterSymbol Is Nothing OrElse typeToGenerateIn.IsErrorType OrElse parameterSymbol.IsErrorType Then
                Return False
            End If

            methodSymbol = GenerateMethodSymbol(typeToGenerateIn, parameterSymbol)
            If Not ValidateTypeToGenerateIn(document.Project.Solution, typeToGenerateIn, True, classInterfaceModuleStructTypes, cancellationToken) Then
                typeToGenerateIn = parameterSymbol
            End If
            Return True
        End Function

        Private Function GenerateMethodSymbol(typeToGenerateIn As INamedTypeSymbol, parameterSymbol As INamedTypeSymbol) As IMethodSymbol
            If typeToGenerateIn.IsGenericType Then
                typeToGenerateIn = typeToGenerateIn.ConstructUnboundGenericType.ConstructedFrom
            End If
            Return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes:=ImmutableArray(Of AttributeData).Empty,
                accessibility:=Nothing,
                modifiers:=Nothing,
                returnType:=typeToGenerateIn,
                refKind:=RefKind.None,
                explicitInterfaceImplementations:=Nothing,
                name:=Nothing,
                typeParameters:=ImmutableArray(Of ITypeParameterSymbol).Empty,
                parameters:=ImmutableArray.Create(CodeGenerationSymbolFactory.CreateParameterSymbol(parameterSymbol, "v")),
                statements:=Nothing,
                handlesExpressions:=Nothing,
                returnTypeAttributes:=Nothing,
                methodKind:=MethodKind.Conversion)
        End Function

        Protected Overrides Function GetExplicitConversionDisplayText(state As AbstractGenerateParameterizedMemberService(Of VisualBasicGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax).State) As String
            Return String.Format(VBFeaturesResources.Generate_narrowing_conversion_in_0, state.TypeToGenerateIn.Name)
        End Function

        Protected Overrides Function GetImplicitConversionDisplayText(state As AbstractGenerateParameterizedMemberService(Of VisualBasicGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax).State) As String
            Return String.Format(VBFeaturesResources.Generate_widening_conversion_in_0, state.TypeToGenerateIn.Name)
        End Function
    End Class
End Namespace
