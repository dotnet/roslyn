' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateTypeParameterList(typeArguments As ImmutableArray(Of ITypeSymbol)) As TypeParameterListSyntax
            Using temp = GetArrayBuilder(Of TypeParameterSyntax)()
                Dim typeParameters = temp.Builder

                For Each typeArg In typeArguments
                    typeParameters.Add(GenerateTypeParameter(
                        CodeGenerator.EnsureIsTypeParameter(typeArg)))
                Next

                Return If(typeParameters.Count = 0,
                    Nothing,
                    TypeParameterList(SeparatedList(typeParameters)))
            End Using
        End Function

        Private Function GenerateTypeParameter(symbol As ITypeParameterSymbol) As TypeParameterSyntax
            Dim variance =
                If(symbol.Variance = VarianceKind.In, Token(SyntaxKind.InKeyword),
                If(symbol.Variance = VarianceKind.Out, Token(SyntaxKind.OutKeyword), Nothing))
            Return TypeParameter(
                variance,
                Identifier(symbol.Name),
                GenerateTypeParameterConstraintClause(symbol))
        End Function

        Private Function GenerateTypeParameterConstraintClause(symbol As ITypeParameterSymbol) As TypeParameterConstraintClauseSyntax
            Using temp = GetArrayBuilder(Of ConstraintSyntax)()
                Dim constraints = temp.Builder

                If symbol.HasConstructorConstraint Then
                    constraints.Add(NewConstraint(Token(SyntaxKind.NewKeyword)))
                End If

                If symbol.HasReferenceTypeConstraint Then
                    constraints.Add(ClassConstraint(Token(SyntaxKind.ClassKeyword)))
                End If

                If symbol.HasValueTypeConstraint Then
                    constraints.Add(StructureConstraint(Token(SyntaxKind.StructureKeyword)))
                End If

                For Each constraint In symbol.ConstraintTypes
                    constraints.Add(TypeConstraint(constraint.GenerateTypeSyntax()))
                Next

                Return If(constraints.Count = 0,
                    Nothing,
                    TypeParameterMultipleConstraintClause(SeparatedList(constraints)))
            End Using
        End Function
    End Module
End Namespace
