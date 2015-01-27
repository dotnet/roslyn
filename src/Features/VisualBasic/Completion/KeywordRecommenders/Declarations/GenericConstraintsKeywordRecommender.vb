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
            recommendations.Add(New RecommendedKeyword("Class", VBFeaturesResources.GenericConstraintsClassKeywordToolTip))
            recommendations.Add(New RecommendedKeyword("Structure", VBFeaturesResources.GenericConstraintsStructureKeywordToolTip))
            recommendations.Add(New RecommendedKeyword("New", VBFeaturesResources.GenericConstraintsNewKeywordToolTip))

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
