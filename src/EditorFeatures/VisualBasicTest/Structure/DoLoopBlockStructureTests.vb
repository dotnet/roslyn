' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class DoLoopBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of DoLoopBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DoLoopBlockStructureProvider()
        End Function

        <Fact>
        Public Async Function TestDoLoopBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:Do While (True) $$
        Loop|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Do While (True) ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestDoLoopBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|span:Do $$
        Loop While (True)|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Do ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
