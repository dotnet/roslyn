' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class FromKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterDimEqualsNewTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = New |</MethodBody>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterFromTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New Foo From |</ClassDeclaration>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterWith1Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterWith2Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New Foo With |</ClassDeclaration>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterDimEqualsNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim x = new C |
    End Sub
End Module</File>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterDimEqualsNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim x = new C() |
    End Sub
End Module</File>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterDimAsNewTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x As New |</MethodBody>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterDimAsNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim x as new C |
    End Sub
End Module</File>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterDimAsNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim x As new C() |
    End Sub
End Module</MethodBody>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterAssignmentNewTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>x = New |</MethodBody>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterAssignmentNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim b = New C |
    End Sub
End Module</File>, "From")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterAssignmentNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub
End Class
Module Program
    Sub Main(args As String())
        Dim x as C
        x = new C() |
    End Sub
End Module</MethodBody>, "From")
        End Function

        <WorkItem(542741)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromAfterLambdaHeaderTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 As Func(Of Integer()) = Function() |</MethodBody>, "From")
        End Function

        <WorkItem(543291)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoFromAfterDotTest() As Task
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            Await VerifyRecommendationsMissingAsync(code, "From")
        End Function

        <WorkItem(542252)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoFromIfNotCollectionInitializerTest() As Task
            Dim code = <File>
System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim y = New Foo() |
    End Sub
End Module
 
Class Foo
End Class
                       </File>

            Await VerifyRecommendationsMissingAsync(code, "From")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub

    Dim b = New C 
|
End Class</File>, "From")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub

    Dim b = New C _
|
End Class</File>, "From")
        End Function

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromForTypeInheritingCollectionInitializerPatternTest() As Task
            Dim code = <File>
Imports System.Collections

Public Class SupportsAdd
    Implements IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Public Sub Add(x As Object)

    End Sub
End Class

Public Class DerivedSupportsAdd
    Inherits SupportsAdd
End Class

Class Program
    Sub Foo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class

                       </File>

            Await VerifyRecommendationsContainAsync(code, "From")
        End Function

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromForTypeInheritingCollectionInitializerPatternInAccessibleTest() As Task
            Dim code = <File>
Imports System.Collections

Public Class SupportsAdd
    Implements IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Protected Sub Add(x As Object)

    End Sub
End Class

Public Class DerivedSupportsAdd
    Inherits SupportsAdd
End Class

Class Program
    Sub Foo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class

                       </File>

            Await VerifyRecommendationsMissingAsync(code, "From")
        End Function

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FromForTypeInheritingCollectionInitializerPatternAccessibleTest() As Task
            Dim code = <File>
Imports System.Collections

Public Class SupportsAdd
    Implements IEnumerable

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function

    Protected Sub Add(x As Object)

    End Sub
End Class

Public Class DerivedSupportsAdd
    Inherits SupportsAdd

    Sub Foo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "From")
        End Function
    End Class
End Namespace
