' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class TypeParameterGenerator
        Public Shared Function GenerateTypeParameterList(typeParameters As ImmutableArray(Of ITypeParameterSymbol)) As TypeParameterListSyntax
            Return If(typeParameters.Length = 0,
                      Nothing,
                      SyntaxFactory.TypeParameterList(parameters:=SyntaxFactory.SeparatedList(typeParameters.Select(AddressOf GenerateTypeParameter))))
        End Function

        Private Shared Function GenerateTypeParameter(symbol As ITypeParameterSymbol) As TypeParameterSyntax
            Return SyntaxFactory.TypeParameter(
                varianceKeyword:=If(symbol.Variance = VarianceKind.In, SyntaxFactory.Token(SyntaxKind.InKeyword), If(symbol.Variance = VarianceKind.Out, SyntaxFactory.Token(SyntaxKind.OutKeyword), Nothing)),
                identifier:=symbol.Name.ToIdentifierToken,
                typeParameterConstraintClause:=GenerateTypeParameterConstraintClause(symbol))
        End Function

        Private Shared Function GenerateTypeParameterConstraintClause(symbol As ITypeParameterSymbol) As TypeParameterConstraintClauseSyntax
            Dim constraints = New List(Of ConstraintSyntax)
            If symbol.HasReferenceTypeConstraint Then
                constraints.Add(SyntaxFactory.ClassConstraint(SyntaxFactory.Token(SyntaxKind.ClassKeyword)))
            End If

            If symbol.HasValueTypeConstraint Then
                constraints.Add(SyntaxFactory.StructureConstraint(SyntaxFactory.Token(SyntaxKind.StructureKeyword)))
            End If

#If False Then
            For Each t In symbol.ConstraintTypes
                Dim typeSyntax = t.GenerateTypeSyntax()
                If Not TypeOf typeSyntax Is ArrayTypeSyntax Then

                End If
            Next
#End If

            constraints.AddRange(symbol.ConstraintTypes.Select(Function(t) SyntaxFactory.TypeConstraint(t.GenerateTypeSyntax())))

            If symbol.HasConstructorConstraint Then
                constraints.Add(SyntaxFactory.NewConstraint(SyntaxFactory.Token(SyntaxKind.NewKeyword)))
            End If

            If constraints.Count = 0 Then
                Return Nothing
            End If

            If constraints.Count = 1 Then
                Return SyntaxFactory.TypeParameterSingleConstraintClause(constraint:=constraints(0))
            End If

            Return SyntaxFactory.TypeParameterMultipleConstraintClause(
                constraints:=SyntaxFactory.SeparatedList(constraints))
        End Function
    End Class
End Namespace
