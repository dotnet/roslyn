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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Implements", VBFeaturesResources.Specifies_one_or_more_interfaces_or_interface_members_that_must_be_implemented_in_the_class_or_structure_definition_in_which_the_Implements_statement_appears))
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

                                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Implements", VBFeaturesResources.Indicates_that_a_class_or_structure_member_is_providing_the_implementation_for_a_member_defined_in_an_interface))
                            End If
                        Next
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
