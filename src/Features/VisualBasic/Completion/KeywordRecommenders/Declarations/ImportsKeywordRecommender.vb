' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Imports" keyword
    ''' </summary>
    Friend Class ImportsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreProcessorDirectiveContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If context.SyntaxTree.HasCompilationUnitRoot Then
                ' Make sure there isn't a Option statement after us
                ' TODO: does this break our rule of not looking forward?
                Dim compilationUnit = DirectCast(context.SyntaxTree.GetRoot(cancellationToken), CompilationUnitSyntax)
                If compilationUnit.Options.Count > 0 Then
                    If context.Position <= compilationUnit.Options.First().SpanStart Then
                        Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                    End If
                End If
            End If

            ' If we have no left token, then we're at the start of the file
            If targetToken.Kind = SyntaxKind.None Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Imports", VBFeaturesResources.ImportsKeywordToolTip))
            End If

            ' Show if after an earlier option statement
            If context.IsAfterStatementOfKind(SyntaxKind.OptionStatement, SyntaxKind.ImportsStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Imports", VBFeaturesResources.ImportsKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
