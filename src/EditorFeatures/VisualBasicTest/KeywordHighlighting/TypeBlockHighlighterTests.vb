' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class TypeBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New TypeBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestClass1()
            Test(<Text>
{|Cursor:[|Class|]|} C1
[|End Class|]</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestClass2()
            Test(<Text>
[|Class|] C1
{|Cursor:[|End Class|]|}</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestModule1()
            Test(<Text>
{|Cursor:[|Module|]|} M1
[|End Module|]</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestModule2()
            Test(<Text>
[|Module|] M1
{|Cursor:[|End Module|]|}</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestStructure1()
            Test(<Text>
{|Cursor:[|Structure|]|} S1
[|End Structure|]</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestStructure2()
            Test(<Text>
[|Structure|] S1
{|Cursor:[|End Structure|]|}</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestInterface1()
            Test(<Text>
{|Cursor:[|Interface|]|} I1
[|End Interface|]</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestInterface2()
            Test(<Text>
[|Interface|] I1
{|Cursor:[|End Interface|]|}</Text>)
        End Sub
    End Class
End Namespace
