' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MultiLineIfBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of MultiLineIfBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MultiLineIfBlockStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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
