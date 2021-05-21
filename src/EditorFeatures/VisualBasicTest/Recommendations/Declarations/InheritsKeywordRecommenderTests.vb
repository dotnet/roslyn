﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InheritsKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterClassDeclarationTest()
            VerifyRecommendationsContain(<File>
Class Goo
|</File>, "Inherits")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInterfaceDeclarationTest()
            VerifyRecommendationsContain(<File>
Interface Goo
|</File>, "Inherits")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterClassDeclarationAndBlankLineTest()
            VerifyRecommendationsContain(<File>
Class Goo

|</File>, "Inherits")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInterfaceDeclarationAndBlankLineTest()
            VerifyRecommendationsContain(<File>
Interface Goo

|</File>, "Inherits")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotAfterImplementsTest()
            VerifyRecommendationsMissing(<File>
Class Goo
Implements IGooable
|</File>, "Inherits")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotInStructureTest()
            VerifyRecommendationsMissing(<File>
Structure Goo
|</File>, "Inherits")
        End Sub

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInheritsInInterfaceTest()
            VerifyRecommendationsContain(<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>, "Inherits")
        End Sub

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotAfterInheritsInClassTest()
            VerifyRecommendationsMissing(<File>
Public Class Goo
    Inherits Bar
    |
</File>, "Inherits")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Inherits")
        End Sub
    End Class
End Namespace
