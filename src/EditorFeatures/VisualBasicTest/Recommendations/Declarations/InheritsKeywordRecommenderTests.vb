' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InheritsKeywordRecommenderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterClassDeclaration()
            VerifyRecommendationsContain(<File>
Class Foo
|</File>, "Inherits")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInterfaceDeclaration()
            VerifyRecommendationsContain(<File>
Interface Foo
|</File>, "Inherits")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterClassDeclarationAndBlankLine()
            VerifyRecommendationsContain(<File>
Class Foo

|</File>, "Inherits")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInterfaceDeclarationAndBlankLine()
            VerifyRecommendationsContain(<File>
Interface Foo

|</File>, "Inherits")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotAfterImplements()
            VerifyRecommendationsMissing(<File>
Class Foo
Implements IFooable
|</File>, "Inherits")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotInStructure()
            VerifyRecommendationsMissing(<File>
Structure Foo
|</File>, "Inherits")
        End Sub

        <WorkItem(531257)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsAfterInheritsInInterface()
            VerifyRecommendationsContain(<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>, "Inherits")
        End Sub

        <WorkItem(531257)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InheritsNotAfterInheritsInClass()
            VerifyRecommendationsMissing(<File>
Public Class Foo
    Inherits Bar
    |
</File>, "Inherits")
        End Sub

        <WorkItem(674791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Inherits")
        End Sub
    End Class
End Namespace
