' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class PropertyDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New PropertyDeclarationHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestAutoProperty1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Property|]|} Foo As Integer [|Implements|] IFoo.Foo
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestAutoProperty2() As Task
            Await TestAsync(<Text>
Class C
[|Public Property|] Foo As Integer {|Cursor:[|Implements|]|} IFoo.Foo
End Class</Text>)
        End Function

    End Class
End Namespace
