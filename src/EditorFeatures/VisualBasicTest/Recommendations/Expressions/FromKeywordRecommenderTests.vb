' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class FromKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterDimEqualsNew()
            VerifyRecommendationsMissing(<MethodBody>Dim x = New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterFrom()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Foo From |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith1()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith2()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Foo With |</ClassDeclaration>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimEqualsNewTypeName()
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
        Public Sub FromAfterDimEqualsNewTypeNameAndParens()
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
        Public Sub NoneAfterDimAsNew()
            VerifyRecommendationsMissing(<MethodBody>Dim x As New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterDimAsNewTypeName()
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
        Public Sub FromAfterDimAsNewTypeNameAndParens()
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
        Public Sub NoneAfterAssignmentNew()
            VerifyRecommendationsMissing(<MethodBody>x = New |</MethodBody>, "From")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterAssignmentNewTypeName()
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
        Public Sub FromAfterAssignmentNewTypeNameAndParens()
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

        <WorkItem(542741)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromAfterLambdaHeader()
            VerifyRecommendationsContain(<MethodBody>Dim q1 As Func(Of Integer()) = Function() |</MethodBody>, "From")
        End Sub

        <WorkItem(543291)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoFromAfterDot()
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(542252)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoFromIfNotCollectionInitializer()
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

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
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

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
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

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPattern()
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

            VerifyRecommendationsContain(code, "From")
        End Sub

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPatternInAccessible()
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

            VerifyRecommendationsMissing(code, "From")
        End Sub

        <WorkItem(4754, "https://github.com/dotnet/roslyn/issues/4754")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FromForTypeInheritingCollectionInitializerPatternAccessible()
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

            VerifyRecommendationsContain(code, "From")
        End Sub
    End Class
End Namespace
