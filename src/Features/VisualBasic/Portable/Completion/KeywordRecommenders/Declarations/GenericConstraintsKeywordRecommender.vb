' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations

    Friend Class GenericConstraintsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            Dim recommendations As New List(Of RecommendedKeyword)
            recommendations.Add(New RecommendedKeyword("Class", VBFeaturesResources.Constrains_a_generic_type_parameter_to_require_that_any_type_argument_passed_to_it_be_a_reference_type))
            recommendations.Add(New RecommendedKeyword("Structure", VBFeaturesResources.Constrains_a_generic_type_parameter_to_require_that_any_type_argument_passed_to_it_be_a_value_type))
            recommendations.Add(New RecommendedKeyword("New", VBFeaturesResources.Specifies_a_constructor_constraint_on_a_generic_type_parameter))

            If targetToken.IsChildToken(Of TypeParameterSingleConstraintClauseSyntax)(Function(constraint) constraint.AsKeyword) Then
                Return recommendations
            ElseIf TypeOf targetToken.Parent Is TypeParameterMultipleConstraintClauseSyntax Then
                Dim multipleConstraint = DirectCast(targetToken.Parent, TypeParameterMultipleConstraintClauseSyntax)
                If targetToken = multipleConstraint.OpenBraceToken OrElse targetToken.Kind = SyntaxKind.CommaToken Then

                    Dim previousConstraints = multipleConstraint.Constraints.Where(Function(c) c.Span.End < context.Position).ToList()

                    ' Structure can only be listed with previous type constraints
                    If previousConstraints.Any(Function(constraint) Not constraint.IsKind(SyntaxKind.TypeConstraint)) Then
                        recommendations.RemoveAll(Function(k) k.Keyword = "Structure")
                    End If
                    If previousConstraints.Any(Function(constraint) constraint.IsKind(SyntaxKind.ClassConstraint, SyntaxKind.StructureConstraint)) Then
                        recommendations.RemoveAll(Function(k) k.Keyword = "Class")
                    End If
                    If previousConstraints.Any(Function(constraint) constraint.IsKind(SyntaxKind.NewConstraint, SyntaxKind.StructureConstraint)) Then
                        recommendations.RemoveAll(Function(k) k.Keyword = "New")
                    End If

                    Return recommendations
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
