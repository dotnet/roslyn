﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class WithBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New WithBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
