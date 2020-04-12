' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class CTypeCastExpressionDocumentation
        Inherits AbstractCastExpressionDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {
                    New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "CType"),
                    New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")
                }
            End Get
        End Property
    End Class
End Namespace
