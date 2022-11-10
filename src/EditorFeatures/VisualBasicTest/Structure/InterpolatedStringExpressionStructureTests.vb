' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class InterpolatedStringExpressionStructureTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of InterpolatedStringExpressionSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New InterpolatedStringExpressionStructureProvider()
        End Function

        <Fact>
        Public Async Function TestMultiLineStringLiteral() As Task
            Const code = "
Class C
    Sub M()
        Dim v =
{|hint:{|textspan:$$$""
{123}
""|}|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestMissingOnIncompleteStringLiteral() As Task
            Const code = "
Class C
    Sub M()
        Dim v =
$$$""
    End Sub
End Class
"

            Await VerifyNoBlockSpansAsync(code)
        End Function
    End Class
End Namespace
