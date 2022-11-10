' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class MultiLineIfBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of MultiLineIfBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MultiLineIfBlockStructureProvider()
        End Function

        <Fact>
        Public Async Function TestIfBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:If (True) $$
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "If (True) ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestIfBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|span:If (True) $$
        ElseIf
        End If|}    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "If (True) ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
