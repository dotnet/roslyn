' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.NavigateTo.Interfaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NavigateTo
    Public Class NavigateToTests
        Inherits AbstractNavigateToTests

        Protected Overrides ReadOnly Property Language As String = "vb"

        Protected Overrides Function CreateWorkspace(content As String, exportProvider As ExportProvider) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(content, exportProvider:=exportProvider)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestNoItemsForEmptyFile() As Task
            Await TestAsync("", Async Function(w)
                                    Assert.Empty(Await _aggregator.GetItemsAsync("Hello"))
                                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClass() As Task
            Await TestAsync(
"Class Goo
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", MatchKind.Exact, NavigateToItemKind.Class)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimClass() As Task
            Await TestAsync(
"Class [Class]
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("class")).Single()
                VerifyNavigateToResultItem(item, "Class", "[|Class|]", MatchKind.Exact, NavigateToItemKind.Class)

                item = (Await _aggregator.GetItemsAsync("[class]")).Single()
                VerifyNavigateToResultItem(item, "Class", "[|Class|]", MatchKind.Exact, NavigateToItemKind.Class)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNestedClass() As Task
            Await TestAsync(
"Class Alpha
Class Beta
Class Gamma
End Class
End Class
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Gamma")).Single()
                VerifyNavigateToResultItem(item, "Gamma", "[|Gamma|]", MatchKind.Exact, NavigateToItemKind.Class)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMemberInANestedClass() As Task
            Await TestAsync("Class Alpha
Class Beta
Class Gamma
Sub DoSomething()
End Sub
End Class
End Class
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", MatchKind.Regular, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Alpha.Beta.Gamma", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedClass() As Task
            Await TestAsync("Class Goo(Of M As IComparable)
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](Of M)", MatchKind.Exact, NavigateToItemKind.Class)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedMethod() As Task
            Await TestAsync("Class Goo(Of M As IComparable)
Public Sub Bar(Of T As IComparable)()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|](Of T)()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo(Of M)", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialClass() As Task
            Await TestAsync("Partial Public Class Goo
Private a As Integer
End Class
Partial Class Goo
Private b As Integer
End Class", Async Function(w)

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Goo")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespace() As Task
            Await TestAsync("Namespace Bar
Class Goo
End Class
End Namespace", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                    VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", MatchKind.Exact, NavigateToItemKind.Class)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStruct() As Task
            Await TestAsync("Structure Bar
End Structure", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemFriend)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("B")).Single()
                    VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", MatchKind.Prefix, NavigateToItemKind.Structure)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnum() As Task
            Await TestAsync("Enum Colors
Red
Green
Blue
End Enum", Async Function(w)
               SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemFriend)
               Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("C")).Single()
               VerifyNavigateToResultItem(item, "Colors", "[|C|]olors", MatchKind.Prefix, NavigateToItemKind.Enum)
           End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnumMember() As Task
            Await TestAsync("Enum Colors
Red
Green
Blue
End Enum", Async Function(w)
               SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic)
               Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("G")).Single()
               VerifyNavigateToResultItem(item, "Green", "[|G|]reen", MatchKind.Prefix, NavigateToItemKind.EnumItem)
           End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField1() As Task
            Await TestAsync("Class Goo
Private Bar As Integer
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Ba|]r", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField2() As Task
            Await TestAsync("Class Goo
Private Bar As Integer
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Ba|]r", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField3() As Task
            Await TestAsync("Class Goo
Private Bar As Integer
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Assert.Empty(Await _aggregator.GetItemsAsync("ar"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimField() As Task
            Await TestAsync("Class Goo
Private [string] As String
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("string")).Single()
                VerifyNavigateToResultItem(item, "string", "[|string|]", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                item = (Await _aggregator.GetItemsAsync("[string]")).Single()
                VerifyNavigateToResultItem(item, "string", "[|string|]", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstField() As Task
            Await TestAsync("Class Goo
Private Const bar As String = ""bar""
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("bar")).Single()
                VerifyNavigateToResultItem(item, "bar", "[|bar|]", MatchKind.Exact, NavigateToItemKind.Constant, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindIndexer() As Task
            Await TestAsync("Class Goo
Private arr As Integer()
Default Public Property Item(ByVal i As Integer) As Integer
Get
Return arr(i)
End Get
Set(ByVal value As Integer)
arr(i) = value
End Set
End Property
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Item")).Single()
                VerifyNavigateToResultItem(item, "Item", "[|Item|](Integer)", MatchKind.Exact, NavigateToItemKind.Property, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo), WorkItem(780993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780993")>
        Public Async Function TestFindEvent() As Task
            Await TestAsync("Class Goo
Public Event Bar as EventHandler
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]", MatchKind.Exact, NavigateToItemKind.Event, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNormalProperty() As Task
            Await TestAsync("Class Goo
Property Name As String
Get
Return String.Empty
End Get
Set(value As String)
End Set
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", "[|Name|]", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindAutoImplementedProperty() As Task
            Await TestAsync("Class Goo
Property Name As String
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", "[|Name|]", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethod() As Task
            Await TestAsync("Class Goo
Private Sub DoSomething()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", MatchKind.Regular, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimMethod() As Task
            Await TestAsync("Class Goo
Private Sub [Sub]()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sub")).Single()
                VerifyNavigateToResultItem(item, "Sub", "[|Sub|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                item = (Await _aggregator.GetItemsAsync("[sub]")).Single()
                VerifyNavigateToResultItem(item, "Sub", "[|Sub|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindParameterizedMethod() As Task
            Await TestAsync("Class Goo
Private Sub DoSomething(ByVal i As Integer, s As String)
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(Integer, String)", MatchKind.Regular, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstructor() As Task
            Await TestAsync("Class Goo
Sub New()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].New()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStaticConstructor() As Task
            Await TestAsync("Class Goo
Shared Sub New()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].New()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDestructor() As Task
            Await TestAsync("Class Goo
Implements IDisposable
Public Sub Dispose() Implements IDisposable.Dispose
End Sub
Protected Overrides Sub Finalize()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemProtected)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Finalize")).Single()
                VerifyNavigateToResultItem(item, "Finalize", "[|Finalize|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                item = (Await _aggregator.GetItemsAsync("Dispose")).Single()
                VerifyNavigateToResultItem(item, "Dispose", "[|Dispose|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethods() As Task
            Await TestAsync("Partial Class Goo
Partial Private Sub Bar()
End Sub
End Class
Partial Class Goo
Private Sub Bar()
End Sub
End Class", Async Function(w)

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodDefinitionOnly() As Task
            Await TestAsync("Partial Class Goo
Partial Private Sub Bar()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodImplementationOnly() As Task
            Await TestAsync("Partial Class Goo
Private Sub Bar()
End Sub
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindOverriddenMethods() As Task
            Await TestAsync("Class BaseGoo
Public Overridable Sub Bar()
End Sub
End Class
Class DerivedGoo
Inherits BaseGoo
Public Overrides Sub Bar()
MyBase.Bar()
End Sub
End Class", Async Function(w)

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()
                VerifyNavigateToResultItems(expecteditems, items)
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern1() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("B.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern2() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("C.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern3() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("B.B.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern4() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("Baz.Quux")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern5() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("G.B.B.Quux")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern6() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem)

                    Dim items = (Await _aggregator.GetItemsAsync("F.F.B.B.Quux")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        <WorkItem(7855, "https://github.com/dotnet/Roslyn/issues/7855")>
        Public Async Function TestDottedPattern7() As Task
            Await TestAsync("namespace Goo
namespace Bar
class Baz(of X, Y, Z)
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("Baz.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindInterface() As Task
            Await TestAsync("Public Interface IGoo
End Interface", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("IG")).Single()
                    VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", MatchKind.Prefix, NavigateToItemKind.Interface)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDelegateInNamespace() As Task
            Await TestAsync("Namespace Goo
Delegate Sub DoStuff()
End Namespace", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupDelegate, StandardGlyphItem.GlyphItemFriend)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DoStuff")).Single()
                    VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", MatchKind.Exact, NavigateToItemKind.Delegate)
                End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLambdaExpression() As Task
            Await TestAsync("Class Goo
Dim sqr As Func(Of Integer, Integer) = Function(x) x*x
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sqr")).Single()
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindModule() As Task
            Await TestAsync("Module ModuleTest
End Module", Async Function(w)
                 SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
                 Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("MT")).Single()
                 VerifyNavigateToResultItem(item, "ModuleTest", "[|M|]odule[|T|]est", MatchKind.Regular, NavigateToItemKind.Module)
             End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLineContinuationMethod() As Task
            Await TestAsync("Class Goo
Public Sub Bar(x as Integer,
y as Integer)
End Sub", Async Function(w)
              SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
              Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
              VerifyNavigateToResultItem(item, "Bar", "[|Bar|](Integer, Integer)", MatchKind.Exact, NavigateToItemKind.Method, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
          End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindArray() As Task
            Await TestAsync("Class Goo
Private itemArray as object()
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("itemArray")).Single
                VerifyNavigateToResultItem(item, "itemArray", "[|itemArray|]", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassAndMethodWithSameName() As Task
            Await TestAsync("Class Goo
End Class
Class Test
Private Sub Goo()
End Sub
End Class", Async Function(w)
                Dim expectedItems = New List(Of NavigateToItem) From
                    {
                        New NavigateToItem("Goo", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                        New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                    }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Goo")).ToList()

                VerifyNavigateToResultItems(expectedItems, items)

            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethodNestedInGenericTypes() As Task
            Await TestAsync("Class A(Of T)
Class B
Structure C(Of U)
Sub M()
End Sub
End Structure
End Class
End Class", Async Function(w)
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item = (Await _aggregator.GetItemsAsync("M")).Single
                VerifyNavigateToResultItem(item, "M", "[|M|]()", MatchKind.Exact, NavigateToItemKind.Method, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "A(Of T).B.C(Of U)", "Test"))
            End Function)
        End Function

        <WorkItem(1111131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1111131")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespaceWithGlobalPrefix() As Task
            Await TestAsync("Namespace Global.MyNS
Public Class C
End Class
End Namespace", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                    Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                    VerifyNavigateToResultItem(item, "C", "[|C|]", MatchKind.Exact, NavigateToItemKind.Class)
                End Function)
        End Function

        <WorkItem(1121267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1121267")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInGlobalNamespace() As Task
            Await TestAsync("Namespace Global
Public Class C(Of T)
End Class
End Namespace", Async Function(w)
                    SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                    Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                    VerifyNavigateToResultItem(item, "C", "[|C|](Of T)", MatchKind.Exact, NavigateToItemKind.Class)
                End Function)
        End Function

        <WorkItem(1834, "https://github.com/dotnet/roslyn/issues/1834")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestConstructorNotParentedByTypeBlock() As Task
            Await TestAsync("Module Program
End Module
Public Sub New()
End Sub", Async Function(w)
              SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
              Assert.Equal(0, (Await _aggregator.GetItemsAsync("New")).Count)
              Dim item = (Await _aggregator.GetItemsAsync("Program")).Single
              VerifyNavigateToResultItem(item, "Program", "[|Program|]", MatchKind.Exact, NavigateToItemKind.Module)
          End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestStartStopSanity() As Task
            ' Verify that multiple calls to start/stop don't blow up
            Await TestAsync("Public Class Goo
End Class", Async Function(w)
                ' Do one query
                Assert.Single(Await _aggregator.GetItemsAsync("Goo"))
                _provider.StopSearch()

                ' Do the same query again, and make sure nothing was left over
                Assert.Single(Await _aggregator.GetItemsAsync("Goo"))
                _provider.StopSearch()

                ' Dispose the provider
                _provider.Dispose()
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDescriptionItems() As Task
            Await TestAsync("
Public Class Goo
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("G")).Single()
                Dim itemDisplay As INavigateToItemDisplay = item.DisplayFactory.CreateItemDisplay(item)

                Dim descriptionItems = itemDisplay.DescriptionItems

                Dim assertDescription As Action(Of String, String) =
                    Sub(label, value)
                        Dim descriptionItem = descriptionItems.Single(Function(i) i.Category.Single().Text = label)
                        Assert.Equal(value, descriptionItem.Details.Single().Text)
                    End Sub

                assertDescription("File:", w.Documents.Single().Name)
                assertDescription("Line:", "2")
                assertDescription("Project:", "Test")
            End Function)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDescriptionItemsFilePath() As Task
            Using workspace = SetupWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="goo\Test1.vb">
Public Class Goo
End Class
                        </Document>
                    </Project>
                </Workspace>)

                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("G")).Single()
                Dim itemDisplay As INavigateToItemDisplay = item.DisplayFactory.CreateItemDisplay(item)

                Dim descriptionItems = itemDisplay.DescriptionItems

                Dim assertDescription As Action(Of String, String) =
                    Sub(label, value)
                        Dim descriptionItem = descriptionItems.Single(Function(i) i.Category.Single().Text = label)
                        Assert.Equal(value, descriptionItem.Details.Single().Text)
                    End Sub

                assertDescription("File:", workspace.Documents.Single().FilePath)
                assertDescription("Line:", "2")
                assertDescription("Project:", "VisualBasicAssembly1")
            End Using
        End Function
    End Class
End Namespace
