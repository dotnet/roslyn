' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_DefaultsSource

        Private Shared Async Function AssertCompletionItemsContainPromotedItem(state As TestState, displayText As String) As Task
            Dim x As Integer
            Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = displayText AndAlso i.DisplayTextSuffix = "" AndAlso Helpers.TryGetOriginalIndexOfPromotedItem(i, x))
        End Function

        Private Shared Sub AssertCompletionItemsInRelativeOrder(state As TestState, expectedItems As List(Of ValueTuple(Of String, Boolean)))

            Dim list = state.GetCompletionItems()
            Dim current = 0
            For Each item In list

                If current = expectedItems.Count Then
                    Exit For
                End If

                Dim currentExpectedItem = expectedItems(current)
                If item.DisplayText = currentExpectedItem.Item1 Then
                    Dim x As Integer
                    Assert.Equal(currentExpectedItem.Item2, Helpers.TryGetOriginalIndexOfPromotedItem(item, x))

                    current += 1
                End If
            Next
        End Sub

        <WpfFact>
        Public Async Function TestNoItemMatchesDefaults_NoMatchingItem() As Task
            ' We are not adding the additional file which contains type MyAB and MyA
            ' the the suggestion from default source doesn't match anything in the completion list.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void MyMethod()
    {
        My$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(MockDefaultSource)}.ToList())

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsDoNotContainAny("MyAB", "MyA", "★ MyAB", "★ MyA")
                Await state.AssertSelectedCompletionItem("MyMethod", isHardSelected:=True)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestNoItemMatchesDefaults_NoDefaultProvided() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class C
{
    void MyMethod()
    {
        My$$
    }
}
                              </Document>)

                ' We are not setting any default
                MockDefaultSource.Defaults = ImmutableArray(Of String).Empty

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsDoNotContainAny("★ MyAB", "★ MyA")
                Await state.AssertCompletionItemsContainAll("MyAB", "MyA", "MyMethod")
                Await state.AssertSelectedCompletionItem("MyA", isHardSelected:=True)

            End Using
        End Function

        <WpfFact>
        <WorkItem(61120, "https://github.com/dotnet/roslyn/issues/61120")>
        Public Async Function SelectFirstMatchingDefaultIfNoFilterText() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class C
{
    void Method()
    {
        $$
    }
}
                              </Document>)

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Dim expectedItems = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ MyAB", isPromoted:=True),
                    CreateExpectedItem("★ MyA", isPromoted:=True),
                    CreateExpectedItem("MyA", isPromoted:=False),
                    CreateExpectedItem("MyAB", isPromoted:=False),
                    CreateExpectedItem("MyMethod", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems)

                Await state.AssertSelectedCompletionItem("★ MyAB", isHardSelected:=True) ' hard-selected since filter text is empty
            End Using
        End Function

        Private Shared Function CreateExpectedItem(displayText As String, isPromoted As Boolean) As ValueTuple(Of String, Boolean)
            Return New ValueTuple(Of String, Boolean)(displayText, isPromoted)
        End Function

        <WpfFact>
        Public Async Function SelectFirstMatchingDefaultWithPrefixFilterText() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class C
{
    void MyMethod()
    {
        My$$
    }
}
                              </Document>)

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Dim expectedItems = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ MyAB", isPromoted:=True),
                    CreateExpectedItem("★ MyA", isPromoted:=True),
                    CreateExpectedItem("MyA", isPromoted:=False),
                    CreateExpectedItem("MyAB", isPromoted:=False),
                    CreateExpectedItem("MyMethod", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems)

                Await state.AssertSelectedCompletionItem("★ MyAB", isHardSelected:=True)
            End Using
        End Function

        <WpfFact>
        Public Async Function DoNotChangeSelectionIfBetterMatch_CaseSensitivePrefix() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class C
{
    void Method(int my)
    {
        m$$
    }
}
                              </Document>)

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Dim expectedItems = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("my", isPromoted:=False),
                    CreateExpectedItem("★ MyA", isPromoted:=True),
                    CreateExpectedItem("MyA", isPromoted:=False),
                    CreateExpectedItem("★ MyAB", isPromoted:=True),
                    CreateExpectedItem("MyAB", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems)
                Await state.AssertSelectedCompletionItem("my", isHardSelected:=True)

            End Using
        End Function

        <WpfFact>
        Public Async Function DoNotChangeSelectionIfBetterMatch_ExactOverPrefix() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class My
{
    void Method()
    {
        My$$
    }
}
                              </Document>)

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Dim expectedItems = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("My", isPromoted:=False),
                    CreateExpectedItem("★ MyA", isPromoted:=True),
                    CreateExpectedItem("MyA", isPromoted:=False),
                    CreateExpectedItem("★ MyAB", isPromoted:=True),
                    CreateExpectedItem("MyAB", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems)
                Await state.AssertSelectedCompletionItem("My", isHardSelected:=True)

            End Using
        End Function

        <WpfFact>
        Public Async Function DoNotChangeIfPreselection() As Task
            Using state = CreateTestStateWithAdditionalDocumentAndMockDefaultSource(
                              <Document>
using NS1;
class C
{
    void Method()
    {
        C x = new $$
    }
}
                              </Document>)

                MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")

                state.SendInvokeCompletionList()

                Dim expectedItems = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ MyAB", isPromoted:=True),
                    CreateExpectedItem("★ MyA", isPromoted:=True),
                    CreateExpectedItem("C", isPromoted:=False),
                    CreateExpectedItem("MyA", isPromoted:=False),
                    CreateExpectedItem("MyAB", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems)

                ' "C" is an item with preselect priority
                Await state.AssertSelectedCompletionItem("C", isHardSelected:=True)
            End Using
        End Function

        Private Shared Function CreateTestStateWithAdditionalDocumentAndMockDefaultSource(documentElement As XElement) As TestState
            Return TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" LanguageVersion="Preview" CommonReferences="true">
                        <ProjectReference>RefProj</ProjectReference>
                        <Document FilePath="C.cs">
                            <%= documentElement.Value %>
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="RefProj" CommonReferences="true">
                        <Document><![CDATA[
namespace NS1
{
    public class MyA
    {
    }
    public class MyAB
    {
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>,
                extraExportedTypes:={GetType(MockDefaultSource)}.ToList())
        End Function

        <ComponentModel.Composition.Export(GetType(IAsyncCompletionDefaultsSource))>
        <VisualStudio.Utilities.Name(NameOf(MockDefaultSource))>
        <VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)>
        <TextViewRole(PredefinedTextViewRoles.Document)>
        Private Class MockDefaultSource
            Implements IAsyncCompletionDefaultsSource

            Public Shared Defaults As ImmutableArray(Of String) = ImmutableArray(Of String).Empty

            <ComponentModel.Composition.ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetSessionDefaultsAsync(session As IAsyncCompletionSession) As Task(Of ImmutableArray(Of String)) Implements IAsyncCompletionDefaultsSource.GetSessionDefaultsAsync
                Return Task.FromResult(Defaults)
            End Function
        End Class

        <WpfFact>
        Public Async Function SelectDefaultItemOverStarredItem_WithPromotion() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class MyClass 
{                          
    public bool FirstStarred()  => true;
    public bool SecondStarred() => true;
    public bool FirstDefault()  => true;
}
class Test
{
    void M(MyClass c)
    {
        c$$
    }
}
                </Document>,
                extraExportedTypes:={GetType(IntelliCodeMockProvider), GetType(MockDefaultSource)}.ToList())

                MockDefaultSource.Defaults = ImmutableArray.Create("FirstDefault")
                state.SendTypeChars(".")

                Dim expectedItems1 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ FirstDefault", isPromoted:=True),
                    CreateExpectedItem("★ FirstStarred", isPromoted:=False),
                    CreateExpectedItem("★ SecondStarred", isPromoted:=False),
                    CreateExpectedItem("FirstDefault", isPromoted:=False),
                    CreateExpectedItem("FirstStarred", isPromoted:=False),
                    CreateExpectedItem("SecondStarred", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems1)
                Await state.AssertSelectedCompletionItem("★ FirstDefault", isHardSelected:=True)

                Dim expectedItems2 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ FirstDefault", isPromoted:=True),
                    CreateExpectedItem("FirstDefault", isPromoted:=False),
                    CreateExpectedItem("★ FirstStarred", isPromoted:=False),
                    CreateExpectedItem("FirstStarred", isPromoted:=False)
                }

                state.SendTypeChars("First")
                AssertCompletionItemsInRelativeOrder(state, expectedItems2)
                Await state.AssertSelectedCompletionItem("★ FirstDefault", isHardSelected:=True)

                Dim expectedItems3 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ FirstStarred", isPromoted:=False),
                    CreateExpectedItem("FirstStarred", isPromoted:=False)
                }

                state.SendTypeChars("S")
                AssertCompletionItemsInRelativeOrder(state, expectedItems3)
                Await state.AssertSelectedCompletionItem("★ FirstStarred", isHardSelected:=True)
            End Using
        End Function

        <WpfFact>
        Public Async Function SelectStarredItemInDefaultList_NoPromotion() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class MyClass 
{                          
    public bool FirstStarred()  => true;
    public bool SecondStarred() => true;
    public bool FirstDefault()  => true;
}
class Test
{
    void M(MyClass c)
    {
        c$$
    }
}
                </Document>,
                extraExportedTypes:={GetType(IntelliCodeMockProvider), GetType(MockDefaultSource)}.ToList())

                MockDefaultSource.Defaults = ImmutableArray.Create("SecondStarred")
                state.SendTypeChars(".")

                Dim expectedItems1 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ FirstStarred", isPromoted:=False),
                    CreateExpectedItem("★ SecondStarred", isPromoted:=False),
                    CreateExpectedItem("FirstDefault", isPromoted:=False),
                    CreateExpectedItem("FirstStarred", isPromoted:=False),
                    CreateExpectedItem("SecondStarred", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems1)
                Await state.AssertSelectedCompletionItem("★ SecondStarred", isHardSelected:=True)

                Dim expectedItems2 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ FirstStarred", isPromoted:=False),
                    CreateExpectedItem("★ SecondStarred", isPromoted:=False),
                    CreateExpectedItem("FirstStarred", isPromoted:=False),
                    CreateExpectedItem("SecondStarred", isPromoted:=False)
                }

                state.SendTypeChars("starred")
                AssertCompletionItemsInRelativeOrder(state, expectedItems2)
                Await state.AssertSelectedCompletionItem("★ SecondStarred", isHardSelected:=True)
            End Using
        End Function

        <ExportCompletionProvider(NameOf(IntelliCodeMockProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class IntelliCodeMockProvider
            Inherits CompletionProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                ' Pythia sets the priority to Preselect
                Dim rules = CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection).WithMatchPriority(MatchPriority.Preselect)
                context.AddItem(CompletionItem.Create(displayText:="★ FirstStarred", filterText:="FirstStarred", sortText:=GetSortText(1), rules:=rules))
                context.AddItem(CompletionItem.Create(displayText:="★ SecondStarred", filterText:="SecondStarred", sortText:=GetSortText(2), rules:=rules))
                Return Task.CompletedTask
            End Function

            ' This is what Pythia uses to sort starred items to the top of completion list
            Private Shared Function GetSortText(index As Integer) As String
                Return "!" + index.ToString("D10")
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

    End Class
End Namespace
