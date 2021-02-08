' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class TryCastExpressionDocumentation
        Inherits AbstractCastExpressionDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Introduces_a_type_conversion_operation_that_does_not_throw_an_exception_If_an_attempted_conversion_fails_TryCast_returns_Nothing_which_your_program_can_test_for
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {
                    New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "TryCast"),
                    New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")
                }
            End Get
        End Property
    End Class
End Namespace
