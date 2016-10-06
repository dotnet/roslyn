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

        Protected Overrides Function CreateWorkspace(content As String, exportProvider As ExportProvider) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateVisualBasicAsync(content, exportProvider:=exportProvider)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestNoItemsForEmptyFile() As Task
            Using worker = Await SetupWorkspaceAsync("")
                Assert.Empty(Await _aggregator.GetItemsAsync("Hello"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClass() As Task
            Using worker = Await SetupWorkspaceAsync(
"Class Foo
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Foo")).Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimClass() As Task
            Using worker = Await SetupWorkspaceAsync(
"Class [Class]
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("class")).Single()
                VerifyNavigateToResultItem(item, "Class", MatchKind.Exact, NavigateToItemKind.Class, displayName:="[Class]")

                item = (Await _aggregator.GetItemsAsync("[class]")).Single()
                VerifyNavigateToResultItem(item, "Class", MatchKind.Exact, NavigateToItemKind.Class, displayName:="[Class]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNestedClass() As Task
            Using worker = Await SetupWorkspaceAsync(
"Class Alpha
Class Beta
Class Gamma
End Class
End Class
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Gamma")).Single()
                VerifyNavigateToResultItem(item, "Gamma", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMemberInANestedClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Alpha
Class Beta
Class Gamma
Sub DoSomething()
End Sub
End Class
End Class
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething()", $"{FeaturesResources.type_space}Alpha.Beta.Gamma")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo(Of M As IComparable)
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Foo")).Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class, displayName:="Foo(Of M)")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo(Of M As IComparable)
Public Sub Bar(Of T As IComparable)()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar(Of T)()", $"{FeaturesResources.type_space}Foo(Of M)")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialClass() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Public Class Foo
Private a As Integer
End Class
Partial Class Foo
Private b As Integer
End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Foo")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Bar
Class Foo
End Class
End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Foo")).Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStruct() As Task
            Using worker = Await SetupWorkspaceAsync("Structure Bar
End Structure")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("B")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Structure)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnum() As Task
            Using worker = Await SetupWorkspaceAsync("Enum Colors
Red
Green
Blue
End Enum")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("C")).Single()
                VerifyNavigateToResultItem(item, "Colors", MatchKind.Prefix, NavigateToItemKind.Enum)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnumMember() As Task
            Using worker = Await SetupWorkspaceAsync("Enum Colors
Red
Green
Blue
End Enum")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("G")).Single()
                VerifyNavigateToResultItem(item, "Green", MatchKind.Prefix, NavigateToItemKind.EnumItem)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField1() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Bar As Integer
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField2() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Bar As Integer
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField3() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Bar As Integer
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Assert.Empty(Await _aggregator.GetItemsAsync("ar"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimField() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private [string] As String
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("string")).Single()
                VerifyNavigateToResultItem(item, "string", MatchKind.Exact, NavigateToItemKind.Field, displayName:="[string]", additionalInfo:=$"{FeaturesResources.type_space}Foo")

                item = (Await _aggregator.GetItemsAsync("[string]")).Single()
                VerifyNavigateToResultItem(item, "string", MatchKind.Exact, NavigateToItemKind.Field, displayName:="[string]", additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstField() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Const bar As String = ""bar""
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("bar")).Single()
                VerifyNavigateToResultItem(item, "bar", MatchKind.Exact, NavigateToItemKind.Constant, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindIndexer() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private arr As Integer()
Default Public Property Item(ByVal i As Integer) As Integer
Get
Return arr(i)
End Get
Set(ByVal value As Integer)
arr(i) = value
End Set
End Property
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Item")).Single()
                VerifyNavigateToResultItem(item, "Item", MatchKind.Exact, NavigateToItemKind.Property, "Item(Integer)", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo), WorkItem(780993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780993")>
        Public Async Function TestFindEvent() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Public Event Bar as EventHandler
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Event, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNormalProperty() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Property Name As String
Get
Return String.Empty
End Get
Set(value As String)
End Set
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindAutoImplementedProperty() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Property Name As String
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Sub DoSomething()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Sub [Sub]()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sub")).Single()
                VerifyNavigateToResultItem(item, "Sub", MatchKind.Exact, NavigateToItemKind.Method, "[Sub]()", $"{FeaturesResources.type_space}Foo")

                item = (Await _aggregator.GetItemsAsync("[sub]")).Single()
                VerifyNavigateToResultItem(item, "Sub", MatchKind.Exact, NavigateToItemKind.Method, "[Sub]()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindParameterizedMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private Sub DoSomething(ByVal i As Integer, s As String)
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething(Integer, String)", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Sub New()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Foo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "New()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStaticConstructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Shared Sub New()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Foo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "Shared New()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDestructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Implements IDisposable
Public Sub Dispose() Implements IDisposable.Dispose
End Sub
Protected Overrides Sub Finalize()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemProtected)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Finalize")).Single()
                VerifyNavigateToResultItem(item, "Finalize", MatchKind.Exact, NavigateToItemKind.Method, "Finalize()", $"{FeaturesResources.type_space}Foo")

                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                item = (Await _aggregator.GetItemsAsync("Dispose")).Single()
                VerifyNavigateToResultItem(item, "Dispose", MatchKind.Exact, NavigateToItemKind.Method, "Dispose()", $"{FeaturesResources.type_space}Foo")

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethods() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo
Partial Private Sub Bar()
End Sub
End Class
Partial Class Foo
Private Sub Bar()
End Sub
End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodDefinitionOnly() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo
Partial Private Sub Bar()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodImplementationOnly() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo
Private Sub Bar()
End Sub
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar()", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindOverriddenMethods() As Task
            Using worker = Await SetupWorkspaceAsync("Class BaseFoo
Public Overridable Sub Bar()
End Sub
End Class
Class DerivedFoo
Inherits BaseFoo
Public Overrides Sub Bar()
MyBase.Bar()
End Sub
End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()
                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern1() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                }

                Dim items = (Await _aggregator.GetItemsAsync("B.Q")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern2() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                }

                Dim items = (Await _aggregator.GetItemsAsync("C.Q")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern3() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                }

                Dim items = (Await _aggregator.GetItemsAsync("B.B.Q")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern4() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items = (Await _aggregator.GetItemsAsync("Baz.Quux")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern5() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items = (Await _aggregator.GetItemsAsync("F.B.B.Quux")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern6() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem)

                Dim items = (Await _aggregator.GetItemsAsync("F.F.B.B.Quux")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        <WorkItem(7855, "https://github.com/dotnet/Roslyn/issues/7855")>
        Public Async Function TestDottedPattern7() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo
namespace Bar
class Baz(of X, Y, Z)
sub Quux()
end sub
end class
end namespace
end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                }

                Dim items = (Await _aggregator.GetItemsAsync("Baz.Q")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindInterface() As Task
            Using worker = Await SetupWorkspaceAsync("Public Interface IFoo
End Interface")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("IF")).Single()
                VerifyNavigateToResultItem(item, "IFoo", MatchKind.Prefix, NavigateToItemKind.Interface)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDelegateInNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Foo
Delegate Sub DoStuff()
End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupDelegate, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DoStuff")).Single()
                VerifyNavigateToResultItem(item, "DoStuff", MatchKind.Exact, NavigateToItemKind.Delegate)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLambdaExpression() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Dim sqr As Func(Of Integer, Integer) = Function(x) x*x
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sqr")).Single()
                VerifyNavigateToResultItem(item, "sqr", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindModule() As Task
            Using worker = Await SetupWorkspaceAsync("Module ModuleTest
End Module")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("MT")).Single()
                VerifyNavigateToResultItem(item, "ModuleTest", MatchKind.Regular, NavigateToItemKind.Module)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLineContinuationMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Public Sub Bar(x as Integer,
y as Integer)
End Sub")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar(Integer, Integer)", $"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindArray() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
Private itemArray as object()
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("itemArray")).Single
                VerifyNavigateToResultItem(item, "itemArray", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=$"{FeaturesResources.type_space}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassAndMethodWithSameName() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo
End Class
Class Test
Private Sub Foo()
End Sub
End Class")
                Dim expectedItems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Foo", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Foo")).ToList()

                VerifyNavigateToResultItems(expectedItems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethodNestedInGenericTypes() As Task
            Using worker = Await SetupWorkspaceAsync("Class A(Of T)
Class B
Structure C(Of U)
Sub M()
End Sub
End Structure
End Class
End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item = (Await _aggregator.GetItemsAsync("M")).Single
                VerifyNavigateToResultItem(item, "M", MatchKind.Exact, NavigateToItemKind.Method, displayName:="M()", additionalInfo:=$"{FeaturesResources.type_space}A(Of T).B.C(Of U)")
            End Using
        End Function

        <WorkItem(1111131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1111131")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespaceWithGlobalPrefix() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Global.MyNS
Public Class C
End Class
End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                VerifyNavigateToResultItem(item, "C", MatchKind.Exact, NavigateToItemKind.Class, displayName:="C")
            End Using
        End Function

        <WorkItem(1121267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1121267")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInGlobalNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Global
Public Class C(Of T)
End Class
End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                VerifyNavigateToResultItem(item, "C", MatchKind.Exact, NavigateToItemKind.Class, displayName:="C(Of T)")
            End Using
        End Function

        <WorkItem(1834, "https://github.com/dotnet/roslyn/issues/1834")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestConstructorNotParentedByTypeBlock() As Task
            Using worker = Await SetupWorkspaceAsync("Module Program
End Module
Public Sub New()
End Sub")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
                Assert.Equal(0, (Await _aggregator.GetItemsAsync("New")).Count)
                Dim item = (Await _aggregator.GetItemsAsync("Program")).Single
                VerifyNavigateToResultItem(item, "Program", MatchKind.Exact, NavigateToItemKind.Module, displayName:="Program")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestStartStopSanity() As Task
            ' Verify that multiple calls to start/stop don't blow up
            Using worker = Await SetupWorkspaceAsync("Public Class Foo
End Class")
                ' Do one query
                Assert.Single(Await _aggregator.GetItemsAsync("Foo"))
                _provider.StopSearch()

                ' Do the same query again, and make sure nothing was left over
                Assert.Single(Await _aggregator.GetItemsAsync("Foo"))
                _provider.StopSearch()

                ' Dispose the provider
                _provider.Dispose()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDescriptionItems() As Task
            Using workspace = Await SetupWorkspaceAsync("
Public Class Foo
End Class")
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("F")).Single()
                Dim itemDisplay As INavigateToItemDisplay = item.DisplayFactory.CreateItemDisplay(item)

                Dim descriptionItems = itemDisplay.DescriptionItems

                Dim assertDescription As Action(Of String, String) =
                    Sub(label, value)
                        Dim descriptionItem = descriptionItems.Single(Function(i) i.Category.Single().Text = label)
                        Assert.Equal(value, descriptionItem.Details.Single().Text)
                    End Sub

                assertDescription("File:", workspace.Documents.Single().Name)
                assertDescription("Line:", "2")
                assertDescription("Project:", "Test")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDescriptionItemsFilePath() As Task
            Using workspace = Await SetupWorkspaceAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="foo\Test1.vb">
Public Class Foo
End Class
                        </Document>
                    </Project>
                </Workspace>)

                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("F")).Single()
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