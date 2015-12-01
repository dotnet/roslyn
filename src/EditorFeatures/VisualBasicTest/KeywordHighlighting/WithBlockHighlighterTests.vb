' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class WithBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New WithBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestWithBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|With|]|} y
.x = 10
Console.WriteLine(.x)
[|End With|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestWithBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|With|] y
.x = 10
Console.WriteLine(.x)
{|Cursor:[|End With|]|}
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
