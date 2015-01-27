' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the various list of operators you can overload after the "Operator" keyword
    ''' </summary>
    Friend Class OverloadableOperatorRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If Not targetToken.IsKind(SyntaxKind.OperatorKeyword) OrElse
               Not targetToken.Parent.IsKind(SyntaxKind.OperatorStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim modifierFacts = context.ModifierCollectionFacts

            ' If we have a Widening or Narrowing declaration, then we must be a CType operator
            If modifierFacts.NarrowingOrWideningKeyword.Kind <> SyntaxKind.None Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("CType", VBFeaturesResources.OperatorCTypeKeywordToolTip))
            Else
                ' We could just be a normal name, so we list all possible options here. Dev10 allows you to type
                ' "Operator Narrowing", so we also list the Narrowing/Widening options as well.
                ' TODO: fix parser to actually deal with such stupidities like "Operator Narrowing"
                Return {"+", "-", "IsFalse", "IsTrue", "Not",
                        "*", "/", "\", "&", "^", ">>", "<<", "=", "<>", ">", ">=", "<", "<=", "And", "Like", "Mod", "Or", "Xor",
                        "Narrowing", "Widening"}.Select(Function(s) New RecommendedKeyword(s, GetToolTipForKeyword(s)))
            End If
        End Function

        Private Function GetToolTipForKeyword([operator] As String) As String
            Select Case [operator]
                Case "+"
                    Return VBFeaturesResources.PlusKeywordToolTip
                Case "-"
                    Return VBFeaturesResources.MinusKeywordToolTip
                Case "IsFalse"
                    Return VBFeaturesResources.IsFalseKeywordToolTip
                Case "IsTrue"
                    Return VBFeaturesResources.IsTrueKeywordToolTip
                Case "Not"
                    Return VBFeaturesResources.NotKeywordToolTip
                Case "*"
                    Return VBFeaturesResources.MultiplicationKeywordToolTip
                Case "/"
                    Return VBFeaturesResources.DivisionKeywordToolTip
                Case "\"
                    Return VBFeaturesResources.IntegerDivisionKeywordToolTip
                Case "&"
                    Return VBFeaturesResources.ConcatKeywordToolTip
                Case "^"
                    Return VBFeaturesResources.ExponentiationKeywordToolTip
                Case ">>"
                    Return VBFeaturesResources.RightShiftKeywordToolTip
                Case "<<"
                    Return VBFeaturesResources.LeftShiftKeywordToolTip
                Case "="
                    Return VBFeaturesResources.EqualsKeywordToolTip
                Case "<>"
                    Return VBFeaturesResources.NotEqualsKeywordToolTip
                Case ">"
                    Return VBFeaturesResources.GreaterThanKeywordToolTip
                Case ">="
                    Return VBFeaturesResources.GreaterThanOrEqualsKeywordToolTip
                Case "<"
                    Return VBFeaturesResources.LessThanKeywordToolTip
                Case "<="
                    Return VBFeaturesResources.LessThanOrEqualsKeywordToolTip
                Case "And"
                    Return VBFeaturesResources.AndKeywordToolTip
                Case "Like"
                    Return VBFeaturesResources.LikeKeywordToolTip
                Case "Mod"
                    Return VBFeaturesResources.ModKeywordToolTip
                Case "Or"
                    Return VBFeaturesResources.OrKeywordToolTip
                Case "Xor"
                    Return VBFeaturesResources.XorKeywordToolTip
                Case "Narrowing"
                    Return VBFeaturesResources.NarrowingKeywordToolTip
                Case "Widening"
                    Return VBFeaturesResources.WideningKeywordToolTip
                Case Else
                    Return String.Empty
            End Select
        End Function
    End Class
End Namespace
