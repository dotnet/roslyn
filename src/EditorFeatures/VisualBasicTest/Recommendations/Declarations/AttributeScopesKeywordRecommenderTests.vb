' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AttributeScopeKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileTest()
            VerifyRecommendationsContain(<File>&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileAfterImportsTest()
            VerifyRecommendationsContain(<File>
Imports Goo
&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileBeforeClassTest()
            VerifyRecommendationsContain(<File>
&lt;|
Class Goo
End Class</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileInsideClassTest()
            VerifyRecommendationsAreExactly(<File>
Class Goo
&lt;|
End Class</File>, {"Global"})
        End Sub

        <WorkItem(542207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542207")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileAtStartOfMalformedAttributeTest()
            VerifyRecommendationsContain(<File><![CDATA[<|Assembly: AssemblyDelaySignAttribute(True)&gt;]]></File>,
                                         "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesAtEndOfFileTest()
            VerifyRecommendationsContain(<File>
Class goo
End Class
&lt;|
</File>, "Assembly", "Module")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesAfterEolTest()
            VerifyRecommendationsContain(<File>
Class goo
End Class
&lt;
|
</File>, "Assembly", "Module")
        End Sub
    End Class
End Namespace
