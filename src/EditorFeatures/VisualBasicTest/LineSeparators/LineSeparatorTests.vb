' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineSeparators
Imports Microsoft.CodeAnalysis.LineSeparators
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineSeparators
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.LineSeparators)>
    Public Class LineSeparatorTests
        <Fact>
        Public Async Function TestNoLinesInEmptyFile() As Task
            Await AssertTagsAsync(Array.Empty(Of TextSpan)(), "")
        End Function

        <Fact>
        Public Async Function TestEmptyClass() As Task
            Await AssertTagsAsync({New TextSpan(9, 9)},
                       "Class C
End Class")
        End Function

        <Fact>
        Public Async Function TestEmptyModule() As Task
            Await AssertTagsAsync({New TextSpan(10, 10)},
                       "Module C
End Module")
        End Function

        <Fact>
        Public Async Function TestEmptyStructure() As Task
            Await AssertTagsAsync({New TextSpan(13, 13)},
                       "Structure S
End Structure")
        End Function

        <Fact>
        Public Async Function TestEmptyInterface() As Task
            Await AssertTagsAsync({New TextSpan(13, 13)},
                       "Interface I
End Interface")
        End Function

        <Fact>
        Public Async Function TestEmptyEnum() As Task
            Await AssertTagsAsync({New TextSpan(8, 8)},
                       "Enum E
End Enum")
        End Function

        <Fact>
        Public Async Function TestEmptyNamespace() As Task
            Await AssertTagsAsync({New TextSpan(13, 13)},
                       "Namespace N
End Namespace")
        End Function

        <Fact>
        Public Async Function TestClassWithOneMethod() As Task
            Await AssertTagsAsync({New TextSpan(40, 9)},
                       "Class C
    Sub Method()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestClassWithTwoMethods() As Task
            Await AssertTagsAsync({
                           New TextSpan(32, 7),
                           New TextSpan(75, 9)
                       },
                       "Class C
    Sub Method1()
    End Sub

    Sub Method2()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestClassWithTwoNonEmptyMethods() As Task
            Await AssertTagsAsync({
                            New TextSpan(45, 7),
                            New TextSpan(101, 9)
                       },
                       "Class C
    Sub Method1()
        M()
    End Sub

    Sub Method2()
        M()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestClassWithMethodAndField() As Task
            Await AssertTagsAsync({
                            New TextSpan(32, 7),
                            New TextSpan(65, 9)
                       },
                       "Class C
    Sub Method1()
    End Sub

    Dim X as Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestClassWithFieldAndMethod() As Task
            Await AssertTagsAsync({
                           New TextSpan(17, 12),
                           New TextSpan(65, 9)
                       },
                       "Class C
    Dim X as Integer

    Sub Method1()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestEmptyClassInNamespace() As Task
            Await AssertTagsAsync({New TextSpan(41, 13)},
                       "Namespace N
    Class C
    End Class
End Namespace")
        End Function

        <Fact>
        Public Async Function TestNamespaceAndTwoClasses() As Task
            Await AssertTagsAsync({
                           New TextSpan(31, 9),
                           New TextSpan(73, 13)
                       },
                       "Namespace N
    Class C1
    End Class

    Class C2
    End Class
End Namespace")
        End Function

        <Fact>
        Public Async Function TestNamespaceAndTwoClassesAndDelegate() As Task
            Await AssertTagsAsync({
                           New TextSpan(31, 9),
                           New TextSpan(62, 9),
                           New TextSpan(97, 13)
                       },
                       "Namespace N
    Class C1
    End Class

    Class C2
    End Class

    Delegate Sub D()
End Namespace")
        End Function

        <Fact>
        Public Async Function TestNestedClass() As Task
            Await AssertTagsAsync({New TextSpan(37, 9)},
                       "Class C
    Class N
    End Class
End Class")
        End Function

        <Fact>
        Public Async Function TestTwoNestedClasses() As Task
            Await AssertTagsAsync({
                           New TextSpan(27, 9),
                           New TextSpan(69, 9)
                       },
                       "Class C
    Class N1
    End Class

    Class N2
    End Class
End Class")
        End Function

        <Fact>
        Public Async Function TestProperty() As Task
            Await AssertTagsAsync({New TextSpan(164, 9)},
                       "Class C
    Property Prop as Integer
        Get
            Return 42
        End Get
        Set(ByVal value as Integer)
        End Set
    End Property
End Class")
        End Function

        <Fact>
        Public Async Function TestPropertyAndField() As Task
            Await AssertTagsAsync({
                           New TextSpan(150, 12),
                           New TextSpan(188, 9)
                       },
                       "Class C
    Property Prop as Integer
        Get
            Return 42
        End Get
        Set(ByVal value as Integer)
        End Set
    End Property

    Dim x as Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestImports() As Task
            Await AssertTagsAsync({
                            New TextSpan(8, 6),
                            New TextSpan(29, 9)
                       },
                       "Imports System

Class Goo
End Class")
        End Function

        <Fact>
        Public Async Function TestCustomEvent() As Task
            Await AssertTagsAsync({
                            New TextSpan(235, 9),
                            New TextSpan(272, 9)
                       },
                       "Class C
    Custom Event E as EventHandler
        AddHandler(value as EventHandler)
        End AddHandler
        RemoveHandler(value as EventHandler)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Dim y as Integer

End Class")
        End Function

        <Fact>
        Public Async Function TestConstructor() As Task
            Await AssertTagsAsync({
                            New TextSpan(26, 7),
                            New TextSpan(59, 9)
                       },
                       "Class C
    Sub New
    End Sub

    Dim y as Integer
End Class")
        End Function

        Private Shared Async Function AssertTagsAsync(spans As IEnumerable(Of TextSpan), content As String) As Tasks.Task
            Dim tags = Await GetSpansForAsync(content)
            Assert.Equal(spans.Count(), tags.Count())

            Dim i As Integer = 0
            For Each span In spans
                Assert.Equal(span, tags.ElementAt(i))
                i = i + 1
            Next
        End Function

        Private Shared Async Function GetSpansForAsync(content As String) As Tasks.Task(Of IEnumerable(Of TextSpan))
            Using workspace = TestWorkspace.CreateVisualBasic(content)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim service = Assert.IsType(Of VisualBasicLineSeparatorService)(workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of ILineSeparatorService)())
                Dim spans = Await service.GetLineSeparatorsAsync(document,
                    (Await document.GetSyntaxRootAsync()).FullSpan, Nothing)
                Return spans.OrderBy(Function(span) span.Start)
            End Using
        End Function
    End Class
End Namespace
