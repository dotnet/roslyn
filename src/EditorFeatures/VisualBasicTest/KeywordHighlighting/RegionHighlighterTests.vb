' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class RegionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(RegionHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|#Region|]|} "Main"
    Sub Main()
    End Sub
[|#End Region|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample1_2() As Task
            Await TestAsync(<Text>
Class C
[|#Region|] "Main"
    Sub Main()
    End Sub
{|Cursor:[|#End Region|]|}
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample2_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|#Region|]|} "Main"
    Sub Main()
#Region "Body"
#End Region
    End Sub
[|#End Region|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample2_2() As Task
            Await TestAsync(<Text>
Class C
#Region "Main"
    Sub Main()
{|Cursor:[|#Region|]|} "Body"
[|#End Region|]
    End Sub
#End Region
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample2_3() As Task
            Await TestAsync(<Text>
Class C
#Region "Main"
    Sub Main()
[|#Region|] "Body"
{|Cursor:[|#End Region|]|}
    End Sub
#End Region
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample2_4() As Task
            Await TestAsync(<Text>
Class C
[|#Region|] "Main"
    Sub Main()
#Region "Body"
#End Region
    End Sub
{|Cursor:[|#End Region|]|}
End Class</Text>)
        End Function
    End Class
End Namespace
