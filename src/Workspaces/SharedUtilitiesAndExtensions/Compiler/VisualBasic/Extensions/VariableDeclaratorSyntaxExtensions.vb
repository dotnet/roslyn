' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module VariableDeclaratorSyntaxExtensions
        <Extension()>
        Public Function HasInitializer(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.Initializer IsNot Nothing OrElse
                   (variableDeclarator.AsClause IsNot Nothing AndAlso
                   TypeOf variableDeclarator.AsClause Is AsNewClauseSyntax)
        End Function

        <Extension()>
        Public Function GetInitializer(variableDeclarator As VariableDeclaratorSyntax) As ExpressionSyntax
            If variableDeclarator.Initializer IsNot Nothing Then
                Return variableDeclarator.Initializer.Value
            ElseIf variableDeclarator.AsClause IsNot Nothing AndAlso TypeOf variableDeclarator.AsClause Is AsNewClauseSyntax Then
                Return DirectCast(variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function Type(variableDeclarator As VariableDeclaratorSyntax, semanticModel As SemanticModel) As ITypeSymbol
            If TypeOf variableDeclarator.AsClause Is AsNewClauseSyntax Then
                Dim asNewType = DirectCast(variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression.Type()
                If asNewType IsNot Nothing Then
                    Return TryCast(semanticModel.GetSymbolInfo(asNewType).Symbol, ITypeSymbol)
                End If
            Else
                ' In the case that the variable declarator doesn't have an As New clause, it should
                ' only have a single name.
                Dim name = variableDeclarator.Names.Single()
                Dim localSymbol = TryCast(semanticModel.GetDeclaredSymbol(name), ILocalSymbol)

                Return localSymbol.Type
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function IsTypeInferred(variableDeclarator As VariableDeclaratorSyntax, semanticModel As SemanticModel) As Boolean
            If Not semanticModel.OptionInfer OrElse variableDeclarator.AsClause IsNot Nothing Then
                Return False
            End If

            If variableDeclarator.IsParentKind(SyntaxKind.FieldDeclaration) AndAlso
               Not DirectCast(variableDeclarator.Parent, FieldDeclarationSyntax).Modifiers.Any(SyntaxKind.ConstKeyword) Then
                Return False
            End If

            Dim initializer = variableDeclarator.GetInitializer()
            If initializer Is Nothing OrElse initializer.IsMissing Then
                Return False
            End If

            Return True
        End Function
    End Module
End Namespace
