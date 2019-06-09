' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DoLoopBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of DoLoopBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DoLoopBlockStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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
