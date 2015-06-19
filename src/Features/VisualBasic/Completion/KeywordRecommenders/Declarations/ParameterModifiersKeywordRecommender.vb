' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the ByVal, ByRef, etc keywords.
    ''' </summary>
    Friend Class ParameterModifiersKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            Dim methodDeclaration = targetToken.GetAncestor(Of MethodBaseSyntax)()
            If methodDeclaration Is Nothing OrElse methodDeclaration.ParameterList Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim parameterAlreadyHasByValOrByRef = False
            If targetToken.GetAncestor(Of ParameterSyntax)() IsNot Nothing Then
                parameterAlreadyHasByValOrByRef = targetToken.GetAncestor(Of ParameterSyntax)().Modifiers.Any(Function(m) m.IsKind(SyntaxKind.ByValKeyword, SyntaxKind.ByRefKeyword))
            End If

            ' Compute some basic properties of what is allowed at all in this context
            Dim byRefAllowed = Not TypeOf methodDeclaration Is AccessorStatementSyntax AndAlso
                               methodDeclaration.Kind <> SyntaxKind.PropertyStatement AndAlso
                               methodDeclaration.Kind <> SyntaxKind.OperatorStatement

            Dim optionalAndParamArrayAllowed = Not TypeOf methodDeclaration Is DelegateStatementSyntax AndAlso
                                               Not TypeOf methodDeclaration Is LambdaHeaderSyntax AndAlso
                                               Not TypeOf methodDeclaration Is AccessorStatementSyntax AndAlso
                                               methodDeclaration.Kind <> SyntaxKind.EventStatement AndAlso
                                               methodDeclaration.Kind <> SyntaxKind.OperatorStatement

            ' Compute a simple list of the "standard" recommendations assuming nothing special is going on
            Dim defaultRecommendations As New List(Of RecommendedKeyword)
            defaultRecommendations.Add(New RecommendedKeyword("ByVal", VBFeaturesResources.ByValKeywordToolTip))

            If byRefAllowed Then
                defaultRecommendations.Add(New RecommendedKeyword("ByRef", VBFeaturesResources.ByRefKeywordToolTip))
            End If

            If optionalAndParamArrayAllowed Then
                defaultRecommendations.Add(New RecommendedKeyword("Optional", VBFeaturesResources.OptionalKeywordToolTip))
                defaultRecommendations.Add(New RecommendedKeyword("ParamArray", VBFeaturesResources.ParamArrayKeywordToolTip))
            End If

            If methodDeclaration.ParameterList.OpenParenToken = targetToken Then
                Return defaultRecommendations
            ElseIf targetToken.Kind = SyntaxKind.CommaToken AndAlso targetToken.Parent.Kind = SyntaxKind.ParameterList Then
                ' Now we get to look at previous declarations and see what might still be valid
                For Each parameter In methodDeclaration.ParameterList.Parameters.Where(Function(p) p.FullSpan.End < context.Position)
                    ' If a previous one had a ParamArray, then nothing is valid anymore, since the ParamArray must
                    ' always be the last parameter
                    If parameter.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.ParamArrayKeyword) Then
                        Return Enumerable.Empty(Of RecommendedKeyword)()
                    End If

                    ' If a previous one had an Optional, then all following must be optional. Following Dev10 behavior,
                    ' we recommend just Optional as a first recommendation
                    If parameter.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.OptionalKeyword) AndAlso optionalAndParamArrayAllowed Then
                        Return defaultRecommendations.Where(Function(k) k.Keyword = "Optional")
                    End If
                Next

                ' We had no special requirements, so return the default set
                Return defaultRecommendations
            ElseIf targetToken.Kind = SyntaxKind.OptionalKeyword AndAlso Not parameterAlreadyHasByValOrByRef Then
                Return defaultRecommendations.Where(Function(k) k.Keyword.StartsWith("By", StringComparison.Ordinal))
            ElseIf targetToken.Kind = SyntaxKind.ParamArrayKeyword AndAlso Not parameterAlreadyHasByValOrByRef Then
                Return defaultRecommendations.Where(Function(k) k.Keyword = "ByVal")
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
