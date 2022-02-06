' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class TypeBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(TypeBlockHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestClass1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Class|]|} C1
[|End Class|]</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestClass2() As Task
            Await TestAsync(<Text>
[|Class|] C1
{|Cursor:[|End Class|]|}</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestModule1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Module|]|} M1
[|End Module|]</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestModule2() As Task
            Await TestAsync(<Text>
[|Module|] M1
{|Cursor:[|End Module|]|}</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestStructure1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Structure|]|} S1
[|End Structure|]</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestStructure2() As Task
            Await TestAsync(<Text>
[|Structure|] S1
{|Cursor:[|End Structure|]|}</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestInterface1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Interface|]|} I1
[|End Interface|]</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestInterface2() As Task
            Await TestAsync(<Text>
[|Interface|] I1
{|Cursor:[|End Interface|]|}</Text>)
        End Function
    End Class
End Namespace
