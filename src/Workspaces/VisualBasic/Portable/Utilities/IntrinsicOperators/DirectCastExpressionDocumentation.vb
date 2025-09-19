' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class DirectCastExpressionDocumentation
        Inherits AbstractCastExpressionDocumentation

        Public Overrides ReadOnly Property DocumentationText As String =
            VBWorkspaceResources.Introduces_a_type_conversion_operation_similar_to_CType_The_difference_is_that_CType_succeeds_as_long_as_there_is_a_valid_conversion_whereas_DirectCast_requires_that_one_type_inherit_from_or_implement_the_other_type

        Public Overrides ReadOnly Property PrefixParts As ImmutableArray(Of SymbolDisplayPart) = ImmutableArray.Create(
            New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "DirectCast"),
            New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))
    End Class
End Namespace
