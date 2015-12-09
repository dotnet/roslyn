' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.NavigateTo.Interfaces
Imports Moq
Imports Roslyn.Test.EditorUtilities.NavigateTo

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NavigateTo
    Public Class NavigateToTests
        Private _glyphServiceMock As New Mock(Of IGlyphService)(MockBehavior.Strict)
        Private _provider As NavigateToItemProvider
        Private _aggregator As NavigateToTestAggregator

        Private Async Function SetupWorkspaceAsync(ParamArray lines As String()) As Task(Of TestWorkspace)
            Dim workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync(lines)
            SetupNavigateTo(workspace)
            Return workspace
        End Function

        Private Sub SetupNavigateTo(workspace As TestWorkspace)
            Dim aggregateListener = New AggregateAsynchronousOperationListener(Array.Empty(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata))(), FeatureAttribute.NavigateTo)
            _provider = New NavigateToItemProvider(workspace, _glyphServiceMock.Object, aggregateListener)
            _aggregator = New NavigateToTestAggregator(_provider)
        End Sub

        Private Async Function SetupWorkspaceAsync(workspaceElement As XElement) As Task(Of TestWorkspace)
            Dim workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceElement)
            SetupNavigateTo(workspace)
            Return workspace
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestNoItemsForEmptyFile() As Task
            Using worker = Await SetupWorkspaceAsync()
                Assert.Empty(_aggregator.GetItems("Hello"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("Foo").Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class [Class]", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("class").Single()
                VerifyNavigateToResultItem(item, "Class", MatchKind.Exact, NavigateToItemKind.Class, displayName:="[Class]")

                item = _aggregator.GetItems("[class]").Single()
                VerifyNavigateToResultItem(item, "Class", MatchKind.Exact, NavigateToItemKind.Class, displayName:="[Class]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNestedClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Alpha", "Class Beta", "Class Gamma", "End Class", "End Class", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Gamma").Single()
                VerifyNavigateToResultItem(item, "Gamma", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMemberInANestedClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Alpha", "Class Beta", "Class Gamma", "Sub DoSomething()", "End Sub", "End Class", "End Class", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("DS").Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething()", $"{EditorFeaturesResources.Type}Alpha.Beta.Gamma")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedClass() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo(Of M As IComparable)", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("Foo").Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class, displayName:="Foo(Of M)")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindGenericConstrainedMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo(Of M As IComparable)", "Public Sub Bar(Of T As IComparable)()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Bar").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar(Of T)()", $"{EditorFeaturesResources.Type}Foo(Of M)")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialClass() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Public Class Foo", "Private a As Integer", "End Class", "Partial Class Foo", "Private b As Integer", "End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = _aggregator.GetItems("Foo").ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Bar", "Class Foo", "End Class", "End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("Foo").Single()
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Class)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStruct() As Task
            Using worker = Await SetupWorkspaceAsync("Structure Bar", "End Structure")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupStruct, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("B").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Structure)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnum() As Task
            Using worker = Await SetupWorkspaceAsync("Enum Colors", "Red", "Green", "Blue", "End Enum")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnum, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("C").Single()
                VerifyNavigateToResultItem(item, "Colors", MatchKind.Prefix, NavigateToItemKind.Enum)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindEnumMember() As Task
            Using worker = Await SetupWorkspaceAsync("Enum Colors", "Red", "Green", "Blue", "End Enum")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("G").Single()
                VerifyNavigateToResultItem(item, "Green", MatchKind.Prefix, NavigateToItemKind.EnumItem)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField1() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Bar As Integer", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("Ba").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField2() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Bar As Integer", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("ba").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Prefix, NavigateToItemKind.Field, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindField3() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Bar As Integer", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Assert.Empty(_aggregator.GetItems("ar"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimField() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private [string] As String", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("string").Single()
                VerifyNavigateToResultItem(item, "string", MatchKind.Exact, NavigateToItemKind.Field, displayName:="[string]", additionalInfo:=$"{EditorFeaturesResources.Type}Foo")

                item = _aggregator.GetItems("[string]").Single()
                VerifyNavigateToResultItem(item, "string", MatchKind.Exact, NavigateToItemKind.Field, displayName:="[string]", additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstField() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Const bar As String = ""bar""", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("bar").Single()
                VerifyNavigateToResultItem(item, "bar", MatchKind.Exact, NavigateToItemKind.Constant, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindIndexer() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private arr As Integer()",
                                       "Default Public Property Item(ByVal i As Integer) As Integer",
                                       "Get", "Return arr(i)", "End Get", "Set(ByVal value As Integer)", "arr(i) = value", "End Set",
                                       "End Property",
                                       "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Item").Single()
                VerifyNavigateToResultItem(item, "Item", MatchKind.Exact, NavigateToItemKind.Property, "Item(Integer)", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo), WorkItem(780993)>
        Public Async Function TestFindEvent() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Public Event Bar as EventHandler", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Bar").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Event, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindNormalProperty() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Property Name As String",
                                          "Get", "Return String.Empty", "End Get",
                                          "Set(value As String)", "End Set", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Name").Single()
                VerifyNavigateToResultItem(item, "Name", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindAutoImplementedProperty() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Property Name As String", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Name").Single()
                VerifyNavigateToResultItem(item, "Name", MatchKind.Exact, NavigateToItemKind.Property, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Sub DoSomething()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("DS").Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindVerbatimMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Sub [Sub]()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("sub").Single()
                VerifyNavigateToResultItem(item, "Sub", MatchKind.Exact, NavigateToItemKind.Method, "[Sub]()", $"{EditorFeaturesResources.Type}Foo")

                item = _aggregator.GetItems("[sub]").Single()
                VerifyNavigateToResultItem(item, "Sub", MatchKind.Exact, NavigateToItemKind.Method, "[Sub]()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindParameterizedMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private Sub DoSomething(ByVal i As Integer, s As String)", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("DS").Single()
                VerifyNavigateToResultItem(item, "DoSomething", MatchKind.Regular, NavigateToItemKind.Method, "DoSomething(Integer, String)", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindConstructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Sub New()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Foo").Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "New()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindStaticConstructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Shared Sub New()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("Foo").Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Foo", MatchKind.Exact, NavigateToItemKind.Method, "Shared New()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDestructor() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Implements IDisposable",
                                       "Public Sub Dispose() Implements IDisposable.Dispose", "End Sub",
                                       "Protected Overrides Sub Finalize()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemProtected)
                Dim item As NavigateToItem = _aggregator.GetItems("Finalize").Single()
                VerifyNavigateToResultItem(item, "Finalize", MatchKind.Exact, NavigateToItemKind.Method, "Finalize()", $"{EditorFeaturesResources.Type}Foo")

                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                item = _aggregator.GetItems("Dispose").Single()
                VerifyNavigateToResultItem(item, "Dispose", MatchKind.Exact, NavigateToItemKind.Method, "Dispose()", $"{EditorFeaturesResources.Type}Foo")

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethods() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo", "Partial Private Sub Bar()", "End Sub", "End Class",
                                       "Partial Class Foo", "Private Sub Bar()", "End Sub", "End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = _aggregator.GetItems("Bar").ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodDefinitionOnly() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo", "Partial Private Sub Bar()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("Bar").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindPartialMethodImplementationOnly() As Task
            Using worker = Await SetupWorkspaceAsync("Partial Class Foo", "Private Sub Bar()", "End Sub", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("Bar").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar()", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindOverriddenMethods() As Task
            Using worker = Await SetupWorkspaceAsync("Class BaseFoo", "Public Overridable Sub Bar()", "End Sub", "End Class",
                                       "Class DerivedFoo", "Inherits BaseFoo", "Public Overrides Sub Bar()", "MyBase.Bar()", "End Sub", "End Class")

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = _aggregator.GetItems("Bar").ToList()
                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern1() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                }

                Dim items = _aggregator.GetItems("B.Q").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern2() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                }

                Dim items = _aggregator.GetItems("C.Q").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern3() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Prefix, True, Nothing)
                }

                Dim items = _aggregator.GetItems("B.B.Q").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern4() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items = _aggregator.GetItems("Baz.Quux").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern5() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items = _aggregator.GetItems("F.B.B.Quux").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern6() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem)

                Dim items = _aggregator.GetItems("F.F.B.B.Quux").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDottedPattern7() As Task
            Using workspace = Await SetupWorkspaceAsync("namespace Foo", "namespace Bar", "class Baz(of X, Y, Z)", "sub Quux()", "end sub", "end class", "end namespace", "end namespace")
                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items = _aggregator.GetItems("Baz.Q").ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindInterface() As Task
            Using worker = Await SetupWorkspaceAsync("Public Interface IFoo", "End Interface")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupInterface, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("IF").Single()
                VerifyNavigateToResultItem(item, "IFoo", MatchKind.Prefix, NavigateToItemKind.Interface)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindDelegateInNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Foo", "Delegate Sub DoStuff()", "End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupDelegate, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("DoStuff").Single()
                VerifyNavigateToResultItem(item, "DoStuff", MatchKind.Exact, NavigateToItemKind.Delegate)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLambdaExpression() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Dim sqr As Func(Of Integer, Integer) = Function(x) x*x", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("sqr").Single()
                VerifyNavigateToResultItem(item, "sqr", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindModule() As Task
            Using worker = Await SetupWorkspaceAsync("Module ModuleTest", "End Module")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
                Dim item As NavigateToItem = _aggregator.GetItems("MT").Single()
                VerifyNavigateToResultItem(item, "ModuleTest", MatchKind.Regular, NavigateToItemKind.Module)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindLineContinuationMethod() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Public Sub Bar(x as Integer,", "y as Integer)", "End Sub")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item As NavigateToItem = _aggregator.GetItems("Bar").Single()
                VerifyNavigateToResultItem(item, "Bar", MatchKind.Exact, NavigateToItemKind.Method, "Bar(Integer, Integer)", $"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindArray() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "Private itemArray as object()", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPrivate)
                Dim item As NavigateToItem = _aggregator.GetItems("itemArray").Single
                VerifyNavigateToResultItem(item, "itemArray", MatchKind.Exact, NavigateToItemKind.Field, additionalInfo:=$"{EditorFeaturesResources.Type}Foo")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassAndMethodWithSameName() As Task
            Using worker = Await SetupWorkspaceAsync("Class Foo", "End Class",
                                       "Class Test", "Private Sub Foo()", "End Sub", "End Class")
                Dim expectedItems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Foo", NavigateToItemKind.Method, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing),
                    New NavigateToItem("Foo", NavigateToItemKind.Class, "vb", Nothing, Nothing, MatchKind.Exact, True, Nothing)
                }

                Dim items As List(Of NavigateToItem) = _aggregator.GetItems("Foo").ToList()

                VerifyNavigateToResultItems(expectedItems, items)

            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindMethodNestedInGenericTypes() As Task
            Using worker = Await SetupWorkspaceAsync("Class A(Of T)", "Class B", "Structure C(Of U)", "Sub M()", "End Sub", "End Structure", "End Class", "End Class")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic)
                Dim item = _aggregator.GetItems("M").Single
                VerifyNavigateToResultItem(item, "M", MatchKind.Exact, NavigateToItemKind.Method, displayName:="M()", additionalInfo:=$"{EditorFeaturesResources.Type}A(Of T).B.C(Of U)")
            End Using
        End Function

        <WorkItem(1111131)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInNamespaceWithGlobalPrefix() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Global.MyNS", "Public Class C", "End Class", "End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item = _aggregator.GetItems("C").Single
                VerifyNavigateToResultItem(item, "C", MatchKind.Exact, NavigateToItemKind.Class, displayName:="C")
            End Using
        End Function

        <WorkItem(1121267)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestFindClassInGlobalNamespace() As Task
            Using worker = Await SetupWorkspaceAsync("Namespace Global", "Public Class C(Of T)", "End Class", "End Namespace")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic)
                Dim item = _aggregator.GetItems("C").Single
                VerifyNavigateToResultItem(item, "C", MatchKind.Exact, NavigateToItemKind.Class, displayName:="C(Of T)")
            End Using
        End Function

        <WorkItem(1834, "https://github.com/dotnet/roslyn/issues/1834")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestConstructorNotParentedByTypeBlock() As Task
            Using worker = Await SetupWorkspaceAsync("Module Program", "End Module", "Public Sub New()", "End Sub")
                SetupVerifiableGlyph(StandardGlyphGroup.GlyphGroupModule, StandardGlyphItem.GlyphItemFriend)
                Assert.Equal(0, _aggregator.GetItems("New").Count)
                Dim item = _aggregator.GetItems("Program").Single
                VerifyNavigateToResultItem(item, "Program", MatchKind.Exact, NavigateToItemKind.Module, displayName:="Program")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestStartStopSanity() As Task
            ' Verify that multiple calls to start/stop don't blow up
            Using worker = Await SetupWorkspaceAsync("Public Class Foo", "End Class")
                ' Do one query
                Assert.Single(_aggregator.GetItems("Foo"))
                _provider.StopSearch()

                ' Do the same query again, and make sure nothing was left over
                Assert.Single(_aggregator.GetItems("Foo"))
                _provider.StopSearch()

                ' Dispose the provider
                _provider.Dispose()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)>
        Public Async Function TestDescriptionItems() As Task
            Using workspace = Await SetupWorkspaceAsync("", "Public Class Foo", "End Class")
                Dim item As NavigateToItem = _aggregator.GetItems("F").Single()
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

                Dim item As NavigateToItem = _aggregator.GetItems("F").Single()
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

        Private Sub VerifyNavigateToResultItems(ByRef expectedItems As List(Of NavigateToItem), ByRef items As List(Of NavigateToItem))
            Assert.Equal(expectedItems.Count(), items.Count())

            For index = 0 To items.Count - 1
                Assert.Equal(expectedItems(index).Name, items(index).Name)
                Assert.Equal(expectedItems(index).MatchKind, items(index).MatchKind)
                Assert.Equal(expectedItems(index).Language, items(index).Language)
                Assert.Equal(expectedItems(index).Kind, items(index).Kind)
                Assert.Equal(expectedItems(index).IsCaseSensitive, items(index).IsCaseSensitive)
            Next
        End Sub

        Private Sub VerifyNavigateToResultItem(ByRef result As NavigateToItem, name As String, matchKind As MatchKind,
                                               navigateToItemKind As String, Optional displayName As String = Nothing,
                                               Optional additionalInfo As String = Nothing)
            ' Verify Symbol Information
            Assert.Equal(name, result.Name)
            Assert.Equal(matchKind, result.MatchKind)
            Assert.Equal("vb", result.Language)
            Assert.Equal(navigateToItemKind, result.Kind)

            ' Verify Display
            Dim itemDisplay As INavigateToItemDisplay = result.DisplayFactory.CreateItemDisplay(result)

            Assert.Equal(If(displayName, name), itemDisplay.Name)

            If additionalInfo IsNot Nothing Then
                Assert.Equal(additionalInfo, itemDisplay.AdditionalInformation)
            End If

            ' Make sure to fetch the glyph
            Dim unused = itemDisplay.Glyph
            _glyphServiceMock.Verify()
        End Sub

        Private Sub SetupVerifiableGlyph(standardGlyphGroup As StandardGlyphGroup, standardGlyphItem As StandardGlyphItem)
            _glyphServiceMock.Setup(Function(service) service.GetGlyph(standardGlyphGroup, standardGlyphItem)) _
                            .Returns(CreateIconBitmapSource()) _
                            .Verifiable()
        End Sub

        Private Function CreateIconBitmapSource() As BitmapSource
            Dim stride As Integer = (PixelFormats.Bgr32.BitsPerPixel \ 8) * 16
            Dim bytes(16 * stride - 1) As Byte
            Return BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgr32, Nothing, bytes, stride)
        End Function
    End Class
End Namespace
