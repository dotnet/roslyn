' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module ExpressionSyntaxExtensions
        <Extension()>
        Public Function WalkUpParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsParentKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression.Parent, ExpressionSyntax)
            End While

            Return expression
        End Function

        <Extension()>
        Public Function WalkDownParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression, ParenthesizedExpressionSyntax).Expression
            End While

            Return expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfDot(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Return _
                (expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression) OrElse
                (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso DirectCast(expression.Parent, MemberAccessExpressionSyntax).Expression Is expression)
        End Function

        <Extension()>
        Public Function IsNewOnRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Dim identifierName = TryCast(expression, IdentifierNameSyntax)
            If identifierName Is Nothing Then
                Return False
            End If

            If String.Compare(identifierName.Identifier.ToString(), "New", StringComparison.OrdinalIgnoreCase) <> 0 Then
                Return False
            End If

            Return identifierName.IsRightSideOfDotOrBang()
        End Function

        <Extension()>
        Public Function IsMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsAnyMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression IsNot Nothing AndAlso
                   TypeOf expression.Parent Is MemberAccessExpressionSyntax AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Return expression.IsAnyMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfDot(expression As ExpressionSyntax) As Boolean
            Return expression.IsMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Right Is expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression
        End Function

        <Extension()>
        Public Function IsAnyLiteralExpression(expression As ExpressionSyntax) As Boolean
            Return expression.IsKind(SyntaxKind.CharacterLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.DateLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.FalseLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NothingLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NumericLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.StringLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.TrueLiteralExpression)
        End Function

        <Extension()>
        Public Function DetermineType(expression As ExpressionSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            ' If a parameter appears to have a void return type, then just use 'object' instead.
            If expression IsNot Nothing Then
                Dim typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken)
                Dim symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken)
                If typeInfo.Type IsNot Nothing AndAlso typeInfo.Type.SpecialType = SpecialType.System_Void Then
                    Return semanticModel.Compilation.ObjectType
                End If

                Dim symbol = If(typeInfo.Type, symbolInfo.GetAnySymbol())
                If symbol IsNot Nothing Then
                    Return symbol.ConvertToType(semanticModel.Compilation)
                End If

                If TypeOf expression Is CollectionInitializerSyntax Then
                    Dim collectionInitializer = DirectCast(expression, CollectionInitializerSyntax)
                    Return DetermineType(collectionInitializer, semanticModel, cancellationToken)
                End If
            End If

            Return semanticModel.Compilation.ObjectType
        End Function

        <Extension()>
        Private Function DetermineType(collectionInitializer As CollectionInitializerSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            Dim rank = 1
            While collectionInitializer.Initializers.Count > 0 AndAlso
                  collectionInitializer.Initializers(0).Kind = SyntaxKind.CollectionInitializer
                rank += 1
                collectionInitializer = DirectCast(collectionInitializer.Initializers(0), CollectionInitializerSyntax)
            End While

            Dim type = collectionInitializer.Initializers.FirstOrDefault().DetermineType(semanticModel, cancellationToken)
            Return semanticModel.Compilation.CreateArrayTypeSymbol(type, rank)
        End Function
    End Module
End Namespace
