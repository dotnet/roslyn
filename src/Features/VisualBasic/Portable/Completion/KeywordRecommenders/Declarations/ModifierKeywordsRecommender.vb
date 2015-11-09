' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Property" keyword in member declaration contexts
    ''' </summary>
    Friend Class ModifierKeywordsRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If Not context.IsTypeMemberDeclarationKeywordContext AndAlso Not context.IsTypeDeclarationKeywordContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim modifierFacts = context.ModifierCollectionFacts
            Dim recommendations As New List(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            Dim innermostDeclaration = GetInnermostDeclarationContext(targetToken)
            Dim innermostDeclarationKind =
                If(innermostDeclaration IsNot Nothing AndAlso innermostDeclaration.Kind <> SyntaxKind.CompilationUnit,
                   innermostDeclaration.Kind,
                   SyntaxKind.NamespaceBlock)

            If modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.None AndAlso Not context.IsInterfaceMemberDeclarationKeywordContext Then
                If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                    recommendations.Add(New RecommendedKeyword("Public", VBFeaturesResources.PublicKeywordToolTip))
                End If

                ' Only "Public" is legal for operators
                If modifierFacts.NarrowingOrWideningKeyword.Kind = SyntaxKind.None Then
                    If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                        recommendations.Add(New RecommendedKeyword("Friend", VBFeaturesResources.FriendKeywordToolTip))
                    End If

                    If modifierFacts.DefaultKeyword.Kind = SyntaxKind.None AndAlso innermostDeclarationKind <> SyntaxKind.NamespaceBlock Then
                        recommendations.Add(New RecommendedKeyword("Private", VBFeaturesResources.PrivateKeywordToolTip))
                    End If
                End If
            End If

            If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.ProtectedMember) Then
                If modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.None Then
                    recommendations.Add(New RecommendedKeyword("Protected", VBFeaturesResources.ProtectedKeywordToolTip))
                    recommendations.Add(New RecommendedKeyword("Protected Friend", VBFeaturesResources.ProtectedFriendKeywordToolTip))
                ElseIf modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.ProtectedKeyword AndAlso Not modifierFacts.HasProtectedAndFriend Then
                    ' We could still have a "Friend" later
                    recommendations.Add(New RecommendedKeyword("Friend", VBFeaturesResources.FriendKeywordToolTip))
                ElseIf modifierFacts.AccessibilityKeyword.Kind = SyntaxKind.FriendKeyword AndAlso Not modifierFacts.HasProtectedAndFriend Then
                    ' We could still have a "Protected" later
                    recommendations.Add(New RecommendedKeyword("Protected", VBFeaturesResources.ProtectedKeywordToolTip))
                End If
            End If

            ' Show "Partial" at the module level. Recommending it before "Private"
            ' is fine, because we'll prettylist it.
            If innermostDeclarationKind = SyntaxKind.ClassBlock OrElse
               innermostDeclarationKind = SyntaxKind.ModuleBlock OrElse
               innermostDeclarationKind = SyntaxKind.StructureBlock OrElse
               innermostDeclarationKind = SyntaxKind.NamespaceBlock Then

                If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class) AndAlso
                    modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword AndAlso
                    modifierFacts.AsyncKeyword.Kind = SyntaxKind.None AndAlso
                    modifierFacts.IteratorKeyword.Kind = SyntaxKind.None Then

                    recommendations.Add(New RecommendedKeyword("Partial", VBFeaturesResources.PartialKeywordToolTip))
                End If
            End If

            If modifierFacts.AsyncKeyword.Kind = SyntaxKind.None AndAlso
               modifierFacts.IteratorKeyword.Kind = SyntaxKind.None Then

                If modifierFacts.MutabilityOrWithEventsKeyword.Kind = SyntaxKind.None Then
                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field) Then
                        recommendations.Add(New RecommendedKeyword("Const", VBFeaturesResources.ConstKeywordToolTip))
                        recommendations.Add(New RecommendedKeyword("WithEvents", VBFeaturesResources.WithEventsKeywordToolTip))
                    End If

                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) OrElse modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Field) Then
                        recommendations.Add(New RecommendedKeyword("ReadOnly", VBFeaturesResources.ReadOnlyKeywordToolTip))
                    End If

                    If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) Then
                        recommendations.Add(New RecommendedKeyword("WriteOnly", VBFeaturesResources.WriteOnlyKeywordToolTip))
                    End If
                End If

                ' Some modifiers cannot appear at the module level
                If innermostDeclarationKind = SyntaxKind.ClassBlock OrElse
                   innermostDeclarationKind = SyntaxKind.InterfaceBlock OrElse
                   innermostDeclarationKind = SyntaxKind.StructureBlock OrElse
                   innermostDeclarationKind = SyntaxKind.NamespaceBlock Then

                    If modifierFacts.InheritenceKeyword.Kind = SyntaxKind.None AndAlso modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Class) Then
                        recommendations.Add(New RecommendedKeyword("MustInherit", VBFeaturesResources.MustInheritKeywordToolTip))
                        recommendations.Add(New RecommendedKeyword("NotInheritable", VBFeaturesResources.NotInheritableKeywordToolTip))
                    End If

                    If modifierFacts.OverridableSharedOrPartialKeyword.Kind = SyntaxKind.None Then
                        If Not context.IsInterfaceMemberDeclarationKeywordContext AndAlso
                           modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.OverridesKeyword Then
                            recommendations.Add(New RecommendedKeyword("Shared", VBFeaturesResources.SharedKeywordToolTip))
                        End If

                        If modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.OverridableMethod) Then
                            recommendations.Add(New RecommendedKeyword("MustOverride", VBFeaturesResources.MustOverrideKeywordToolTip))

                            If modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.ShadowsKeyword Then
                                recommendations.Add(New RecommendedKeyword("NotOverridable", VBFeaturesResources.NotOverridableKeywordToolTip))
                            End If

                            If modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.OverridesKeyword Then
                                recommendations.Add(New RecommendedKeyword("Overridable", VBFeaturesResources.OverridableKeywordToolTip))
                            End If
                        End If
                    End If

                    If modifierFacts.OverridesOrShadowsKeyword.Kind = SyntaxKind.None Then
                        If modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.OverridableKeyword AndAlso
                           modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Property) AndAlso
                           Not context.IsInterfaceMemberDeclarationKeywordContext AndAlso
                           modifierFacts.SharedKeyword.Kind = SyntaxKind.None AndAlso
                           modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword Then
                            recommendations.Add(New RecommendedKeyword("Overrides", VBFeaturesResources.OverridesKeywordToolTip))
                        End If

                        If modifierFacts.OverloadsKeyword.Kind = SyntaxKind.None AndAlso
                           modifierFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.NotOverridableKeyword AndAlso
                           modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Property Or PossibleDeclarationTypes.Operator) Then
                            recommendations.Add(New RecommendedKeyword("Shadows", VBFeaturesResources.ShadowsKeywordToolTip))
                        End If
                    End If

                    If modifierFacts.OverloadsKeyword.Kind = SyntaxKind.None AndAlso
                       modifierFacts.OverridesOrShadowsKeyword.Kind <> SyntaxKind.ShadowsKeyword AndAlso
                       modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property Or PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.Operator) Then
                        recommendations.Add(New RecommendedKeyword("Overloads", VBFeaturesResources.OverloadsKeywordToolTip))
                    End If

                    If modifierFacts.DefaultKeyword.Kind = SyntaxKind.None AndAlso
                       modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Property) AndAlso
                       modifierFacts.AccessibilityKeyword.Kind <> SyntaxKind.PrivateKeyword Then
                        recommendations.Add(New RecommendedKeyword("Default", VBFeaturesResources.DefaultKeywordToolTip))
                    End If

                    If modifierFacts.NarrowingOrWideningKeyword.Kind = SyntaxKind.None AndAlso modifierFacts.CouldApplyToOneOf(PossibleDeclarationTypes.Operator) Then
                        recommendations.Add(New RecommendedKeyword("Narrowing", VBFeaturesResources.NarrowingKeywordToolTip))
                        recommendations.Add(New RecommendedKeyword("Widening", VBFeaturesResources.WideningKeywordToolTip))
                    End If
                End If
            End If

            Return recommendations
        End Function
    End Class
End Namespace
