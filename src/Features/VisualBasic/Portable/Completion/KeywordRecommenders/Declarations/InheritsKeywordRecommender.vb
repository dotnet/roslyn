' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Inherits" keyword.
    ''' </summary>
    Friend Class InheritsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' Inherits must be the first thing in the class, by rule.
            If context.IsAfterStatementOfKind(SyntaxKind.ClassStatement, SyntaxKind.InterfaceStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Inherits", VBFeaturesResources.InheritsKeywordToolTip))
            End If

            ' Inherits may also after other Inherits statements in an interface
            Dim typeBlock = context.TargetToken.GetAncestor(Of TypeBlockSyntax)()
            If context.IsAfterStatementOfKind(SyntaxKind.InheritsStatement) AndAlso
               TypeOf typeBlock Is InterfaceBlockSyntax Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Inherits", VBFeaturesResources.InheritsKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
