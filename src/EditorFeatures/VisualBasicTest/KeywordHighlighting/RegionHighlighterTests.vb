' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class RegionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New RegionHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample1_1()
            Test(<Text>
Class C
{|Cursor:[|#Region|]|} "Main"
    Sub Main()
    End Sub
[|#End Region|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample1_2()
            Test(<Text>
Class C
[|#Region|] "Main"
    Sub Main()
    End Sub
{|Cursor:[|#End Region|]|}
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample2_1()
            Test(<Text>
Class C
{|Cursor:[|#Region|]|} "Main"
    Sub Main()
#Region "Body"
#End Region
    End Sub
[|#End Region|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample2_2()
            Test(<Text>
Class C
#Region "Main"
    Sub Main()
{|Cursor:[|#Region|]|} "Body"
[|#End Region|]
    End Sub
#End Region
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample2_3()
            Test(<Text>
Class C
#Region "Main"
    Sub Main()
[|#Region|] "Body"
{|Cursor:[|#End Region|]|}
    End Sub
#End Region
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestRegionSample2_4()
            Test(<Text>
Class C
[|#Region|] "Main"
    Sub Main()
#Region "Body"
#End Region
    End Sub
{|Cursor:[|#End Region|]|}
End Class</Text>)
        End Sub
    End Class
End Namespace
