' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Implements" keyword
    ''' </summary>
    Friend Class ImplementsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            Dim typeBlock = targetToken.GetAncestor(Of TypeBlockSyntax)()
            If TypeOf typeBlock Is InterfaceBlockSyntax Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.IsAfterStatementOfKind(
                    SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.ImplementsStatement, SyntaxKind.InheritsStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Implements", VBFeaturesResources.ImplementsKeywordAfterTypeDeclarationToolTip))
            End If

            If context.IsFollowingParameterListOrAsClauseOfMethodDeclaration() OrElse
               context.IsFollowingCompletePropertyDeclaration(cancellationToken) OrElse
               context.IsFollowingCompleteEventDeclaration() Then

                If typeBlock IsNot Nothing Then
                    ' We need to check to see if any of the partial types parts declare an implements statement.
                    ' If not, we don't show the Implements keyword.
                    Dim typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeBlock)
                    If typeSymbol IsNot Nothing Then
                        For Each reference In typeSymbol.DeclaringSyntaxReferences
                            Dim typeStatement = TryCast(reference.GetSyntax(), TypeStatementSyntax)

                            If typeStatement IsNot Nothing AndAlso
                               TypeOf typeStatement.Parent Is TypeBlockSyntax AndAlso
                               DirectCast(typeStatement.Parent, TypeBlockSyntax).Implements.Count > 0 Then

                                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Implements", VBFeaturesResources.ImplementsKeywordAfterMethodDeclarationToolTip))
                            End If
                        Next
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
