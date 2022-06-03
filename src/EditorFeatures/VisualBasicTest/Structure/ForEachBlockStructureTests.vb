' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class ForEachBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of ForEachBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ForEachBlockStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestForEachBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:For Each v in x $$
        Next|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "For Each v in x ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
