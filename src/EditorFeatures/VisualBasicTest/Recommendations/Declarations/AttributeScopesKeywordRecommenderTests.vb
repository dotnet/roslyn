' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AttributeScopeKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFile()
            VerifyRecommendationsContain(<File>&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileAfterImports()
            VerifyRecommendationsContain(<File>
Imports Foo
&lt;|</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileBeforeClass()
            VerifyRecommendationsContain(<File>
&lt;|
Class Foo
End Class</File>, "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileInsideClass()
            VerifyRecommendationsAreExactly(<File>
Class Foo
&lt;|
End Class</File>, {"Global"})
        End Sub

        <WorkItem(542207)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesInFileAtStartOfMalformedAttribute()
            VerifyRecommendationsContain(<File><![CDATA[<|Assembly: AssemblyDelaySignAttribute(True)&gt;]]></File>,
                                         "Assembly", "Module")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesAtEndOfFile()
            VerifyRecommendationsContain(<File>
Class foo
End Class
&lt;|
</File>, "Assembly", "Module")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AttributeScopesAfterEol()
            VerifyRecommendationsContain(<File>
Class foo
End Class
&lt;
|
</File>, "Assembly", "Module")
        End Sub

    End Class
End Namespace
