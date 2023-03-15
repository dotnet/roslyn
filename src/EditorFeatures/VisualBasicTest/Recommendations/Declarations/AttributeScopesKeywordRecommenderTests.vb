' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AttributeScopeKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AttributeScopesInFileTest()
            VerifyRecommendationsContain(<File>&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        Public Sub AttributeScopesInFileAfterImportsTest()
            VerifyRecommendationsContain(<File>
Imports Goo
&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        Public Sub AttributeScopesInFileBeforeClassTest()
            VerifyRecommendationsContain(<File>
&lt;|
Class Goo
End Class</File>, "Assembly", "Module")
        End Sub

        <Fact>
        Public Sub AttributeScopesInFileInsideClassTest()
            VerifyRecommendationsAreExactly(<File>
Class Goo
&lt;|
End Class</File>, {"Global"})
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542207")>
        Public Sub AttributeScopesInFileAtStartOfMalformedAttributeTest()
            VerifyRecommendationsContain(<File><![CDATA[<|Assembly: AssemblyDelaySignAttribute(True)&gt;]]></File>,
                                         "Assembly", "Module")
        End Sub

        <Fact>
        Public Sub AttributeScopesAtEndOfFileTest()
            VerifyRecommendationsContain(<File>
Class goo
End Class
&lt;|
</File>, "Assembly", "Module")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
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
