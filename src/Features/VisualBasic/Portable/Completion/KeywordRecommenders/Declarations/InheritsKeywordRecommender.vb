' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Inherits", VBFeaturesResources.Causes_the_current_class_or_interface_to_inherit_the_attributes_variables_properties_procedures_and_events_from_another_class_or_set_of_interfaces))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            ' Inherits must be the first thing in the class, by rule.
            If context.IsAfterStatementOfKind(SyntaxKind.ClassStatement, SyntaxKind.InterfaceStatement) Then
                Return s_keywords
            End If

            ' Inherits may also after other Inherits statements in an interface
            Dim typeBlock = context.TargetToken.GetAncestor(Of TypeBlockSyntax)()
            If context.IsAfterStatementOfKind(SyntaxKind.InheritsStatement) AndAlso
               TypeOf typeBlock Is InterfaceBlockSyntax Then

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
