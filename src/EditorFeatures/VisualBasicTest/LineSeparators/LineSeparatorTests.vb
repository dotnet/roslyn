' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineSeparators
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineSeparators
    Public Class LineSeparatorTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub NoLinesInEmptyFile()
            AssertTags(Array.Empty(Of TextSpan)(), "")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyClass()
            AssertTags({New TextSpan(9, 9)},
                       "Class C",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyModule()
            AssertTags({New TextSpan(10, 10)},
                       "Module C",
                       "End Module")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyStructure()
            AssertTags({New TextSpan(13, 13)},
                       "Structure S",
                       "End Structure")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyInterface()
            AssertTags({New TextSpan(13, 13)},
                       "Interface I",
                       "End Interface")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyEnum()
            AssertTags({New TextSpan(8, 8)},
                       "Enum E",
                       "End Enum")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyNamespace()
            AssertTags({New TextSpan(13, 13)},
                       "Namespace N",
                       "End Namespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub ClassWithOneMethod()
            AssertTags({New TextSpan(40, 9)},
                       "Class C",
                       "    Sub Method()",
                       "    End Sub",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub ClassWithTwoMethods()
            AssertTags({
                           New TextSpan(32, 7),
                           New TextSpan(75, 9)
                       },
                       "Class C",
                       "    Sub Method1()",
                       "    End Sub",
                       "",
                       "    Sub Method2()",
                       "    End Sub",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub ClassWithTwoNonEmptyMethods()
            AssertTags({
                            New TextSpan(45, 7),
                            New TextSpan(101, 9)
                       },
                       "Class C",
                       "    Sub Method1()",
                       "        M()",
                       "    End Sub",
                       "",
                       "    Sub Method2()",
                       "        M()",
                       "    End Sub",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub ClassWithMethodAndField()
            AssertTags({
                            New TextSpan(32, 7),
                            New TextSpan(65, 9)
                       },
                       "Class C",
                       "    Sub Method1()",
                       "    End Sub",
                       "",
                       "    Dim X as Integer",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub ClassWithFieldAndMethod()
            AssertTags({
                           New TextSpan(17, 12),
                           New TextSpan(65, 9)
                       },
                       "Class C",
                       "    Dim X as Integer",
                       "",
                       "    Sub Method1()",
                       "    End Sub",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub EmptyClassInNamespace()
            AssertTags({New TextSpan(41, 13)},
                       "Namespace N",
                       "    Class C",
                       "    End Class",
                       "End Namespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub NamespaceAndTwoClasses()
            AssertTags({
                           New TextSpan(31, 9),
                           New TextSpan(73, 13)
                       },
                       "Namespace N",
                       "    Class C1",
                       "    End Class",
                       "",
                       "    Class C2",
                       "    End Class",
                       "End Namespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub NamespaceAndTwoClassesAndDelegate()
            AssertTags({
                           New TextSpan(31, 9),
                           New TextSpan(62, 9),
                           New TextSpan(97, 13)
                       },
                       "Namespace N",
                       "    Class C1",
                       "    End Class",
                       "",
                       "    Class C2",
                       "    End Class",
                       "",
                       "    Delegate Sub D()",
                       "End Namespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub NestedClass()
            AssertTags({New TextSpan(37, 9)},
                       "Class C",
                       "    Class N",
                       "    End Class",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TwoNestedClasses()
            AssertTags({
                           New TextSpan(27, 9),
                           New TextSpan(69, 9)
                       },
                       "Class C",
                       "    Class N1",
                       "    End Class",
                       "",
                       "    Class N2",
                       "    End Class",
                       "End Class")
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TestProperty()
            AssertTags({New TextSpan(164, 9)},
                       "Class C",
                       "    Property Prop as Integer",
                       "        Get",
                       "            Return 42",
                       "        End Get",
                       "        Set(ByVal value as Integer)",
                       "        End Set",
                       "    End Property",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TestPropertyAndField()
            AssertTags({
                           New TextSpan(150, 12),
                           New TextSpan(188, 9)
                       },
                       "Class C",
                       "    Property Prop as Integer",
                       "        Get",
                       "            Return 42",
                       "        End Get",
                       "        Set(ByVal value as Integer)",
                       "        End Set",
                       "    End Property",
                       "",
                       "    Dim x as Integer",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TestImports()
            AssertTags({
                            New TextSpan(8, 6),
                            New TextSpan(29, 9)
                       },
                       "Imports System",
                       "",
                       "Class Foo",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TestCustomEvent()
            AssertTags({
                            New TextSpan(235, 9),
                            New TextSpan(272, 9)
                       },
                       "Class C",
                       "    Custom Event E as EventHandler",
                       "        AddHandler(value as EventHandler)",
                       "        End AddHandler",
                       "        RemoveHandler(value as EventHandler)",
                       "        End RemoveHandler",
                       "        RaiseEvent()",
                       "        End RaiseEvent",
                       "    End Event",
                       "",
                       "    Dim y as Integer",
                       "",
                       "End Class")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)>
        Public Sub TestConstructor()
            AssertTags({
                            New TextSpan(26, 7),
                            New TextSpan(59, 9)
                       },
                       "Class C",
                       "    Sub New",
                       "    End Sub",
                       "",
                       "    Dim y as Integer",
                       "End Class")
        End Sub
        Private Sub AssertTags(spans As IEnumerable(Of TextSpan), ParamArray lines As String())
            Dim tags = GetSpansFor(lines)
            Assert.Equal(spans.Count(), tags.Count())

            Dim i As Integer = 0
            For Each span In spans
                Assert.Equal(span, tags.ElementAt(i))
                i = i + 1
            Next
        End Sub

        Private Function GetSpansFor(ParamArray lines As String()) As IEnumerable(Of TextSpan)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim spans = New VisualBasicLineSeparatorService().GetLineSeparatorsAsync(document, document.GetSyntaxTreeAsync().Result.GetRoot().FullSpan).Result
                Return spans.OrderBy(Function(span) span.Start)
            End Using
        End Function
    End Class
End Namespace
