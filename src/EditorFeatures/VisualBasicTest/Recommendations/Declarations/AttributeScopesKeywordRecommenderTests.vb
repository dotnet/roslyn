' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

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
Imports Foo
&lt;|</File>, "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileBeforeClassTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
&lt;|
Class Foo
End Class</File>, "Assembly", "Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesInFileInsideClassTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<File>
Class Foo
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
Class foo
End Class
&lt;|
</File>, "Assembly", "Module")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AttributeScopesAfterEolTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class foo
End Class
&lt;
|
</File>, "Assembly", "Module")
        End Function
    End Class
End Namespace
