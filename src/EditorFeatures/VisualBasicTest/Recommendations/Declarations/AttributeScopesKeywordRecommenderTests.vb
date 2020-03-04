﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AttributeScopeKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>&lt;|</File>, "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileAfterImportsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Imports Goo
&lt;|</File>, "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileBeforeClassTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
&lt;|
Class Goo
End Class</File>, "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileInsideClassTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<File>
Class Goo
&lt;|
End Class</File>, {"Global"})
        End Function

        <WorkItem(542207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542207")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileAtStartOfMalformedAttributeTest() As Task
            Await VerifyRecommendationsContainAsync(<File><![CDATA[<|Assembly: AssemblyDelaySignAttribute(True)&gt;]]></File>,
                                         "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesAtEndOfFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class goo
End Class
&lt;|
</File>, "Assembly", "Module")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesAfterEolTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class goo
End Class
&lt;
|
</File>, "Assembly", "Module")
        End Function
    End Class
End Namespace
