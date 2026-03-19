' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class EventDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(EventDeclarationHighlighter)
        End Function

        <Fact>
        Public Async Function TestEventSample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Event|]|} Goo() [|Implements|] I1.Goo
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestEventSample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Event|] Goo() {|Cursor:[|Implements|]|} I1.Goo
End Class</Text>)
        End Function
    End Class
End Namespace
