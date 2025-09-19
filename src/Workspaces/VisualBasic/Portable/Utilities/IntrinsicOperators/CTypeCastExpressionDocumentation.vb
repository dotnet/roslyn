' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class CTypeCastExpressionDocumentation
        Inherits AbstractCastExpressionDocumentation

        Public Overrides ReadOnly Property DocumentationText As String =
            VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type

        Public Overrides ReadOnly Property PrefixParts As ImmutableArray(Of SymbolDisplayPart) = ImmutableArray.Create(
            New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "CType"),
            New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))
    End Class
End Namespace
