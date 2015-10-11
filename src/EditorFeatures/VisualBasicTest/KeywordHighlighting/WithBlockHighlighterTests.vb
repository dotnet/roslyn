' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class WithBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New WithBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWithBlock1()
            Test(<Text>
Class C
Sub M()
{|Cursor:[|With|]|} y
.x = 10
Console.WriteLine(.x)
[|End With|]
End Sub
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWithBlock2()
            Test(<Text>
Class C
Sub M()
[|With|] y
.x = 10
Console.WriteLine(.x)
{|Cursor:[|End With|]|}
End Sub
End Class</Text>)
        End Sub
    End Class
End Namespace
