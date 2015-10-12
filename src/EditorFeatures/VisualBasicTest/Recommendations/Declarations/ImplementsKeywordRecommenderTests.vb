' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ImplementsKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterClassDeclaration()
            VerifyRecommendationsContain(<File>
Class Foo
|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterClassDeclarationAndBlankLine()
            VerifyRecommendationsContain(<File>
Class Foo

|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterImplements()
            VerifyRecommendationsContain(<File>
Class Foo
Implements IFooable
|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInStructure()
            VerifyRecommendationsContain(<File>
Structure Foo
|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterInherits()
            VerifyRecommendationsContain(<File>
Class Foo
Inherits Base
|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterMethodInClassImplementingInterface()
            VerifyRecommendationsContain(<File>
Class Foo
Implements IFooable
Sub Foo() |
|</File>, "Implements")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsNotAfterMethodInClassNotImplementingInterface()
            VerifyRecommendationsMissing(<File>
Class Foo
Sub Foo() |
|</File>, "Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyName()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyOpenParen()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x( |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyCloseParen()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAs()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsClause()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterIncompletePropertyAsClauseInitializer()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer = |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsClauseInitializer()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer = 3 |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterIncompletePropertyAsNewClause()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsNewClause()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsNewClauseOpenParen()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object( |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyAsNewClauseCloseParen()
            VerifyRecommendationsContain(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object() |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsNamespaceDot()
            VerifyRecommendationsMissing(
<File>
Interface foo
    Property x() As System.Collections.Generic.List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As System.|
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsListOf()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface foo
    Property x() As List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As List(Of |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsListOfType()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface foo
    Property x() As List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As List(Of bar |
</File>,
"Implements")
        End Sub

        <WorkItem(543635)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyParameter()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface foo
    Property x(i As Integer) As Integer
End Interface
Class bar
    Implements foo
    Property x(i As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(543811)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterEventName()
            VerifyRecommendationsContain(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent |
</File>,
"Implements")
        End Sub

        <WorkItem(543811)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterEventOpenParen()
            VerifyRecommendationsMissing(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent( |
</File>,
"Implements")
        End Sub

        <WorkItem(543811)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterEventCloseParen()
            VerifyRecommendationsContain(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent() |
</File>,
"Implements")
        End Sub

        <WorkItem(546404)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterAsClause()
            VerifyRecommendationsContain(
<File>
Interface I1
    Function F() As Integer
End Interface
Class Bar
    Implements I1
    Function F() As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(531254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInPartialClass1()
            VerifyRecommendationsContain(
<File>
Public Interface ITest
End Interface
Partial Public Class Test
    Implements ITest
End Class
Partial Public Class Test
    Sub X() |
</File>,
"Implements")
        End Sub

        <WorkItem(531254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInPartialClass2()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest
End Interface
Partial Public Class Test
End Class
Partial Public Class Test
    Sub X() |
</File>,
"Implements")
        End Sub

        <WorkItem(531257)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInInterface1()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    |
</File>,
"Implements")
        End Sub

        <WorkItem(531257)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInInterface2()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>,
"Implements")
        End Sub

        <WorkItem(531257)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInModule()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Module Test2
    |
</File>,
"Implements")
        End Sub

        <WorkItem(674791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Implements")
        End Sub
    End Class
End Namespace
