' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class FromKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "From")
        End Sub

        <Fact>
        Public Sub NoneAfterDimEqualsNewTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub NoneAfterFromTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo From |</ClassDeclaration>, "From")
        End Sub

        <Fact>
        Public Sub NoneAfterWith1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "From")
        End Sub

        <Fact>
        Public Sub NoneAfterWith2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo With |</ClassDeclaration>, "From")
        End Sub

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Sub NoneAfterDimAsNewTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New |</MethodBody>, "From")
        End Sub

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Sub NoneAfterAssignmentNewTest()
            VerifyRecommendationsMissing(<MethodBody>x = New |</MethodBody>, "From")
        End Sub

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542741")>
        Public Sub FromAfterLambdaHeaderTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 As Func(Of Integer()) = Function() |</MethodBody>, "From")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543291")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542252")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
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

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4754")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4754")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4754")>
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
