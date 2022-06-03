' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class FromKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterDimEqualsNewTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterFromTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo From |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo With |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimEqualsNewTypeNameTest()
            VerifyRecommendationsContain(<File>Imports System.Collections.Generic
                                             
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimEqualsNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<File>Imports System.Collections.Generic
                                             
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterDimAsNewTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimAsNewTypeNameTest()
            VerifyRecommendationsContain(<File>Imports System.Collections.Generic
                                             
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimAsNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<MethodBody>Imports System.Collections.Generic
                                             
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterAssignmentNewTest()
            VerifyRecommendationsMissing(<MethodBody>x = New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterAssignmentNewTypeNameTest()
            VerifyRecommendationsContain(<File>Imports System.Collections.Generic
                                             
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterAssignmentNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<MethodBody>Imports System.Collections.Generic
                                             
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
        End Sub

        <WorkItem(542741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542741")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterLambdaHeaderTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 As Func(Of Integer()) = Function() |</MethodBody>, "From")
        End Sub

        <WorkItem(543291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543291")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoFromAfterDotTest()
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(542252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542252")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoFromIfNotCollectionInitializerTest()
            Dim code = <File>
System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim y = New Goo() |
    End Sub
End Module
 
Class Goo
End Class
                       </File>

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub

    Dim b = New C 
|
End Class</File>, "From")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub

    Dim b = New C _
|
End Class</File>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<File>Imports System.Collections.Generic
                                             
Class C
    Implements IEnumerable(Of Integer)

    Public Sub Add(i As Integer)
    End Sub

    Dim b = New C _ ' Test
|
End Class</File>, "From")
        End Sub

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPatternTest()
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
    Sub Goo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class

                       </File>

            VerifyRecommendationsContain(code, "From")
        End Sub

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPatternInAccessibleTest()
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
    Sub Goo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class

                       </File>

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPatternAccessibleTest()
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

    Sub Goo()
        Dim x = New DerivedSupportsAdd |
    End Sub
End Class</File>

            VerifyRecommendationsContain(code, "From")
        End Sub
    End Class
End Namespace
