' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.NavigateTo.Interfaces
Imports Microsoft.VisualStudio.Text.PatternMatching

#Disable Warning BC40000 ' MatchKind is obsolete

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NavigateTo
    <Trait(Traits.Feature, Traits.Features.NavigateTo)>
    Public Class NavigateToTests
        Inherits AbstractNavigateToTests

        Protected Overrides ReadOnly Property Language As String = "vb"

        Protected Overrides Function CreateWorkspace(content As String, exportProvider As ExportProvider) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(content, exportProvider:=exportProvider)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestNoItemsForEmptyFile(testHost As TestHost) As Task
            Await TestAsync(testHost, "", Async Function(w)
                                              Assert.Empty(Await _aggregator.GetItemsAsync("Hello"))
                                          End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindClass(testHost As TestHost) As Task
            Await TestAsync(testHost,
"Class Goo
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindVerbatimClass(testHost As TestHost) As Task
            Await TestAsync(testHost,
"Class [Class]
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("class")).Single()
                VerifyNavigateToResultItem(item, "Class", "[|Class|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal)

                item = (Await _aggregator.GetItemsAsync("[class]")).Single()
                VerifyNavigateToResultItem(item, "Class", "[|Class|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindNestedClass(testHost As TestHost) As Task
            Await TestAsync(testHost,
"Class Alpha
Class Beta
Class Gamma
End Class
End Class
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Gamma")).Single()
                VerifyNavigateToResultItem(item, "Gamma", "[|Gamma|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassPublic)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindMemberInANestedClass(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Alpha
Class Beta
Class Gamma
Sub DoSomething()
End Sub
End Class
End Class
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method,
                                           Glyph.MethodPublic, String.Format(FeaturesResources.in_0_project_1, "Alpha.Beta.Gamma", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindGenericConstrainedClass(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo(Of M As IComparable)
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|](Of M)", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindGenericConstrainedMethod(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo(Of M As IComparable)
Public Sub Bar(Of T As IComparable)()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|](Of T)()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, String.Format(FeaturesResources.in_0_project_1, "Goo(Of M)", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindPartialClass(testHost As TestHost) As Task
            Await TestAsync(testHost, "Partial Public Class Goo
Private a As Integer
End Class
Partial Class Goo
Private b As Integer
End Class", Async Function(w)

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing),
                    New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Goo")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindClassInNamespace(testHost As TestHost) As Task
            Await TestAsync(testHost, "Namespace Bar
Class Goo
End Class
End Namespace", Async Function(w)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single()
                    VerifyNavigateToResultItem(item, "Goo", "[|Goo|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindStruct(testHost As TestHost) As Task
            Await TestAsync(testHost, "Structure Bar
End Structure", Async Function(w)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("B")).Single()
                    VerifyNavigateToResultItem(item, "Bar", "[|B|]ar", PatternMatchKind.Prefix, NavigateToItemKind.Structure, Glyph.StructureInternal)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindEnum(testHost As TestHost) As Task
            Await TestAsync(testHost, "Enum Colors
Red
Green
Blue
End Enum", Async Function(w)
               Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("C")).Single()
               VerifyNavigateToResultItem(item, "Colors", "[|C|]olors", PatternMatchKind.Prefix, NavigateToItemKind.Enum, Glyph.EnumInternal)
           End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindEnumMember(testHost As TestHost) As Task
            Await TestAsync(testHost, "Enum Colors
Red
Green
Blue
End Enum", Async Function(w)
               Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("G")).Single()
               VerifyNavigateToResultItem(item, "Green", "[|G|]reen", PatternMatchKind.Prefix, NavigateToItemKind.EnumItem, Glyph.EnumMemberPublic)
           End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindField1(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Bar As Integer
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindField2(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Bar As Integer
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("ba")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Ba|]r", PatternMatchKind.Prefix, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindField3(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Bar As Integer
End Class", Async Function(w)
                Assert.Empty(Await _aggregator.GetItemsAsync("ar"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindVerbatimField(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private [string] As String
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("string")).Single()
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                item = (Await _aggregator.GetItemsAsync("[string]")).Single()
                VerifyNavigateToResultItem(item, "string", "[|string|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindConstField(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Const bar As String = ""bar""
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("bar")).Single()
                VerifyNavigateToResultItem(item, "bar", "[|bar|]", PatternMatchKind.Exact, NavigateToItemKind.Constant, Glyph.ConstantPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindIndexer(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
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
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Item")).Single()
                VerifyNavigateToResultItem(item, "Item", "[|Item|](Integer)", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        <WorkItem(780993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780993")>
        Public Async Function TestFindEvent(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Public Event Bar as EventHandler
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]", PatternMatchKind.Exact, NavigateToItemKind.Event, Glyph.EventPublic, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindNormalProperty(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Property Name As String
Get
Return String.Empty
End Get
Set(value As String)
End Set
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", "[|Name|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindAutoImplementedProperty(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Property Name As String
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Name")).Single()
                VerifyNavigateToResultItem(item, "Name", "[|Name|]", PatternMatchKind.Exact, NavigateToItemKind.Property, Glyph.PropertyPublic, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindMethod(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Sub DoSomething()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething()", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindVerbatimMethod(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Sub [Sub]()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sub")).Single()
                VerifyNavigateToResultItem(item, "Sub", "[|Sub|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                item = (Await _aggregator.GetItemsAsync("[sub]")).Single()
                VerifyNavigateToResultItem(item, "Sub", "[|Sub|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindParameterizedMethod(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private Sub DoSomething(ByVal i As Integer, s As String)
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DS")).Single()
                VerifyNavigateToResultItem(item, "DoSomething", "[|D|]o[|S|]omething(Integer, String)", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindConstructor(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Sub New()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].New()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindStaticConstructor(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Shared Sub New()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Goo")).Single(Function(i) i.Kind = NavigateToItemKind.Method)
                VerifyNavigateToResultItem(item, "Goo", "[|Goo|].New()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindDestructor(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Implements IDisposable
Public Sub Dispose() Implements IDisposable.Dispose
End Sub
Protected Overrides Sub Finalize()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Finalize")).Single()
                VerifyNavigateToResultItem(item, "Finalize", "[|Finalize|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodProtected, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

                item = (Await _aggregator.GetItemsAsync("Dispose")).Single()
                VerifyNavigateToResultItem(item, "Dispose", "[|Dispose|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))

            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindPartialMethods(testHost As TestHost) As Task
            Await TestAsync(testHost, "Partial Class Goo
Partial Private Sub Bar()
End Sub
End Class
Partial Class Goo
Private Sub Bar()
End Sub
End Class", Async Function(w)

                Dim expecteditems = New List(Of NavigateToItem) From
                {
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()

                VerifyNavigateToResultItems(expecteditems, items)

            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindPartialMethodDefinitionOnly(testHost As TestHost) As Task
            Await TestAsync(testHost, "Partial Class Goo
Partial Private Sub Bar()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindPartialMethodImplementationOnly(testHost As TestHost) As Task
            Await TestAsync(testHost, "Partial Class Goo
Private Sub Bar()
End Sub
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
                VerifyNavigateToResultItem(item, "Bar", "[|Bar|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPrivate, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindOverriddenMethods(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class BaseGoo
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
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing),
                    New NavigateToItem("Bar", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Bar")).ToList()
                VerifyNavigateToResultItems(expecteditems, items)
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern1(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyPrefixPatternMatch, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("B.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern2(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
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

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern3(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyPrefixPatternMatch, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("B.B.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern4(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("Baz.Quux")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern5(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
namespace Bar
class Baz
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("G.B.B.Quux")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestDottedPattern6(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
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

        <Theory>
        <CombinatorialData>
        <WorkItem(7855, "https://github.com/dotnet/Roslyn/issues/7855")>
        Public Async Function TestDottedPattern7(testHost As TestHost) As Task
            Await TestAsync(testHost, "namespace Goo
namespace Bar
class Baz(of X, Y, Z)
sub Quux()
end sub
end class
end namespace
end namespace", Async Function(w)
                    Dim expecteditems = New List(Of NavigateToItem) From
                        {
                            New NavigateToItem("Quux", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyPrefixPatternMatch, Nothing)
                        }

                    Dim items = (Await _aggregator.GetItemsAsync("Baz.Q")).ToList()

                    VerifyNavigateToResultItems(expecteditems, items)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindInterface(testHost As TestHost) As Task
            Await TestAsync(testHost, "Public Interface IGoo
End Interface", Async Function(w)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("IG")).Single()
                    VerifyNavigateToResultItem(item, "IGoo", "[|IG|]oo", PatternMatchKind.Prefix, NavigateToItemKind.Interface, Glyph.InterfacePublic)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindDelegateInNamespace(testHost As TestHost) As Task
            Await TestAsync(testHost, "Namespace Goo
Delegate Sub DoStuff()
End Namespace", Async Function(w)
                    Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("DoStuff")).Single()
                    VerifyNavigateToResultItem(item, "DoStuff", "[|DoStuff|]", PatternMatchKind.Exact, NavigateToItemKind.Delegate, Glyph.DelegateInternal)
                End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindLambdaExpression(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Dim sqr As Func(Of Integer, Integer) = Function(x) x*x
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("sqr")).Single()
                VerifyNavigateToResultItem(item, "sqr", "[|sqr|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindModule(testHost As TestHost) As Task
            Await TestAsync(testHost, "Module ModuleTest
End Module", Async Function(w)
                 Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("MT")).Single()
                 VerifyNavigateToResultItem(item, "ModuleTest", "[|M|]odule[|T|]est", PatternMatchKind.CamelCaseExact, NavigateToItemKind.Module, Glyph.ModuleInternal)
             End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindLineContinuationMethod(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Public Sub Bar(x as Integer,
y as Integer)
End Sub", Async Function(w)
              Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("Bar")).Single()
              VerifyNavigateToResultItem(item, "Bar", "[|Bar|](Integer, Integer)", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
          End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindArray(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
Private itemArray as object()
End Class", Async Function(w)
                Dim item As NavigateToItem = (Await _aggregator.GetItemsAsync("itemArray")).Single
                VerifyNavigateToResultItem(item, "itemArray", "[|itemArray|]", PatternMatchKind.Exact, NavigateToItemKind.Field, Glyph.FieldPrivate, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "Goo", "Test"))
            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindClassAndMethodWithSameName(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class Goo
End Class
Class Test
Private Sub Goo()
End Sub
End Class", Async Function(w)
                Dim expectedItems = New List(Of NavigateToItem) From
                    {
                        New NavigateToItem("Goo", NavigateToItemKind.Method, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing),
                        New NavigateToItem("Goo", NavigateToItemKind.Class, "vb", Nothing, Nothing, s_emptyExactPatternMatch, Nothing)
                    }

                Dim items As List(Of NavigateToItem) = (Await _aggregator.GetItemsAsync("Goo")).ToList()

                VerifyNavigateToResultItems(expectedItems, items)

            End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestFindMethodNestedInGenericTypes(testHost As TestHost) As Task
            Await TestAsync(testHost, "Class A(Of T)
Class B
Structure C(Of U)
Sub M()
End Sub
End Structure
End Class
End Class", Async Function(w)
                Dim item = (Await _aggregator.GetItemsAsync("M")).Single
                VerifyNavigateToResultItem(item, "M", "[|M|]()", PatternMatchKind.Exact, NavigateToItemKind.Method, Glyph.MethodPublic, additionalInfo:=String.Format(FeaturesResources.in_0_project_1, "A(Of T).B.C(Of U)", "Test"))
            End Function)
        End Function

        <WorkItem(1111131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1111131")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestFindClassInNamespaceWithGlobalPrefix(testHost As TestHost) As Task
            Await TestAsync(testHost, "Namespace Global.MyNS
Public Class C
End Class
End Namespace", Async Function(w)
                    Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                    VerifyNavigateToResultItem(item, "C", "[|C|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassPublic)
                End Function)
        End Function

        <WorkItem(1121267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1121267")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestFindClassInGlobalNamespace(testHost As TestHost) As Task
            Await TestAsync(testHost, "Namespace Global
Public Class C(Of T)
End Class
End Namespace", Async Function(w)
                    Dim item = (Await _aggregator.GetItemsAsync("C")).Single
                    VerifyNavigateToResultItem(item, "C", "[|C|](Of T)", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassPublic)
                End Function)
        End Function

        <WorkItem(1834, "https://github.com/dotnet/roslyn/issues/1834")>
        <Theory>
        <CombinatorialData>
        Public Async Function TestConstructorNotParentedByTypeBlock(testHost As TestHost) As Task
            Await TestAsync(testHost, "Module Program
End Module
Public Sub New()
End Sub", Async Function(w)
              Assert.Equal(0, (Await _aggregator.GetItemsAsync("New")).Count)
              Dim item = (Await _aggregator.GetItemsAsync("Program")).Single
              VerifyNavigateToResultItem(item, "Program", "[|Program|]", PatternMatchKind.Exact, NavigateToItemKind.Module, Glyph.ModuleInternal)
          End Function)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestStartStopSanity(testHost As TestHost) As Task
            ' Verify that multiple calls to start/stop don't blow up
            Await TestAsync(testHost, "Public Class Goo
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

        <Theory>
        <CombinatorialData>
        Public Async Function TestDescriptionItems(testHost As TestHost) As Task
            Await TestAsync(testHost, "
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

        <Theory>
        <CombinatorialData>
        Public Async Function TestDescriptionItemsFilePath(testHost As TestHost) As Task
            Using workspace = CreateWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="goo\Test1.vb">
Public Class Goo
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost, createTrackingService:=Nothing)

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
#Enable Warning BC40000 ' MatchKind is obsolete
