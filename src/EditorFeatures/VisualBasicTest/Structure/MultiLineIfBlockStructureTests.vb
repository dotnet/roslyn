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
        {|FullSpan:If (True) $$
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("FullSpan", "If (True) ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining), Trait(Traits.Feature, Traits.Features.AdditionalInternalStructureOutlinings)>
        Public Async Function TestIfBlock1a() As Task
            Const code = "
Class C
    Sub M()
        {|FullSpan:If (True)$$
{|PreBlock:            '
            Console.WriteLine()
            '|}
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("FullSpan", "If (True) ...", autoCollapse:=False),
                Region("PreBlock", "...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining), Trait(Traits.Feature, Traits.Features.AdditionalInternalStructureOutlinings)>
        Public Async Function TestIfBlock2() As Task
            Const code = "
Class C
    Sub M()
        {|FullSpan:If (True)
{|PreBlock:             '$$
            Console.WriteLine()
            '|}
        {|PostBlock:Else
            '
            Console.WriteLine()
            '|}
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("FullSpan", "If (True) ...", autoCollapse:=False),
                Region("PreBlock", "...", autoCollapse:=False),
                Region("PostBlock", "Else", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining), Trait(Traits.Feature, Traits.Features.AdditionalInternalStructureOutlinings)>
        Public Async Function TestIfBlock3() As Task
            Const code = "
Class C
    Sub M()
        {|FullSpan:If (True) $$
{|PreBlock:            '
            Console.WriteLine()
            '|}
        {|InnerBlock0:Else If
            '
            Console.WriteLine()
            '|}
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("FullSpan", "If (True) ...", autoCollapse:=False),
                Region("PreBlock", "...", autoCollapse:=False),
                Region("InnerBlock0", "Else If", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining), Trait(Traits.Feature, Traits.Features.AdditionalInternalStructureOutlinings)>
        Public Async Function TestIfBlock4() As Task
            Const code = "
Class C
    Sub M()
        {|FullSpan:If (True) $$
{|PreBlock:            '
            Console.WriteLine()
            '|}
            {|InnerBlock0:Else If
            '
            Console.WriteLine()
            '|}
            {|PostBlock:Else
            '
            Console.WriteLine()
            '|}
        End If|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("FullSpan", "If (True) ...", autoCollapse:=False),
                Region("PreBlock", "...", autoCollapse:=False),
                Region("InnerBlock0", "Else If", autoCollapse:=False),
                Region("PostBlock", "Else", autoCollapse:=False))
        End Function
    End Class
End Namespace
