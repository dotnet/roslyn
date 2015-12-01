' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class TypeBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New TypeBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestClass1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Class|]|} C1
[|End Class|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestClass2() As Task
            Await TestAsync(<Text>
[|Class|] C1
{|Cursor:[|End Class|]|}</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestModule1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Module|]|} M1
[|End Module|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestModule2() As Task
            Await TestAsync(<Text>
[|Module|] M1
{|Cursor:[|End Module|]|}</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestStructure1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Structure|]|} S1
[|End Structure|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestStructure2() As Task
            Await TestAsync(<Text>
[|Structure|] S1
{|Cursor:[|End Structure|]|}</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestInterface1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Interface|]|} I1
[|End Interface|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestInterface2() As Task
            Await TestAsync(<Text>
[|Interface|] I1
{|Cursor:[|End Interface|]|}</Text>)
        End Function
    End Class
End Namespace
