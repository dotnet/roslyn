' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class EventDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New EventDeclarationHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventSample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Event|]|} Foo() [|Implements|] I1.Foo
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventSample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Event|] Foo() {|Cursor:[|Implements|]|} I1.Foo
End Class</Text>)
        End Function
    End Class
End Namespace
