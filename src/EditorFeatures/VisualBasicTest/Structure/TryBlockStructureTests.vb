' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class TryBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of TryBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New TryBlockStructureProvider()
        End Function

        <Fact>
        Public Async Function TestTryBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:Try $$
        Catch (e As Exception)
        End Try|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Try ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestTryBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|span:Try $$
        Finally
        End Try|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Try ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
