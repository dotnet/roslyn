' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class RegionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New RegionHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|#Region|]|} "Main"
    Sub Main()
    End Sub
[|#End Region|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestRegionSample1_2() As Task
            Await TestAsync(<Text>
Class C
[|#Region|] "Main"
    Sub Main()
    End Sub
{|Cursor:[|#End Region|]|}
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
