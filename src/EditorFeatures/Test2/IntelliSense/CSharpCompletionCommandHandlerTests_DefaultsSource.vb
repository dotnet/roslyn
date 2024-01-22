' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.CSharp.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.TestHooks
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/61120")>
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

                Await state.AssertSelectedCompletionItem("★ MyAB", isHardSelected:=False)
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
                Await state.AssertSelectedCompletionItem("★ FirstDefault", isHardSelected:=False)

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
                Await state.AssertSelectedCompletionItem("★ SecondStarred", isHardSelected:=False)

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

        <WpfFact>
        Public Async Function CommitPromotedItemShouldNotIncludeStar() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class Test
{
    void M()
    {
        $$
    }
}
                </Document>,
                extraExportedTypes:={GetType(MockDefaultSource)}.ToList())

                MockDefaultSource.Defaults = ImmutableArray.Create("if")
                state.SendInvokeCompletionList()

                Dim expectedItems1 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ if", isPromoted:=True),
                    CreateExpectedItem("if", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems1)
                Await state.AssertSelectedCompletionItem("★ if", isHardSelected:=False)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Dim committedLine = state.GetLineTextFromCaretPosition()
                Assert.DoesNotContain("★", committedLine, StringComparison.Ordinal)
                Assert.Contains("if", committedLine, StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAutoHidingExpandedItems(afterDot As Boolean, responsiveTypingEnabled As Boolean) As Task
            Dim text As String
            If afterDot Then
                text = "this."
            Else
                text = String.Empty
            End If

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                    <%= text %>$$
                </Document>,
                excludedTypes:={GetType(CSharpCompletionService.Factory)}.ToList(),
                extraExportedTypes:={GetType(MockCompletionServiceFactory)}.ToList())

                Dim workspace = state.Workspace
                workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, responsiveTypingEnabled)

                Dim completionService = DirectCast(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), MockCompletionServiceFactory.Service)

                If Not responsiveTypingEnabled And afterDot Then
                    ' we'd block on expanded items when responsive completion is disabled and triggered after a dot
                    ' so make sure the checkpoint is released first
                    completionService.ExpandedItemsCheckpoint.Release()

                    Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                    state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                    Dim items = state.GetCompletionItems()
                    Assert.Equal(completionService.RegularCount + completionService.ExpandedCount, items.Count)
                    Assert.Equal(completionService.ExpandedCount, items.Where(Function(x) x.Flags.IsExpanded()).Count())
                Else
                    ' we blocked the expanded item task, so only regular items in the list
                    ' this is true even with responsive typing disabled, since the filter text is empty.
                    Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                    state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                    Dim items = state.GetCompletionItems()
                    Assert.Equal(completionService.RegularCount, items.Count)
                    Assert.False(items.Any(Function(i) i.Flags.IsExpanded()))

                    ' make sure expanded item task completes
                    completionService.ExpandedItemsCheckpoint.Release()
                    Dim session = Await state.GetCompletionSession()
                    Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                    Assert.NotNull(sessionData.ExpandedItemsTask)
                    Await sessionData.ExpandedItemsTask
                End If

                Dim typedChars = "Item"
                For i = 0 To typedChars.Length - 1
                    Await state.SendTypeCharsAndWaitForUiRenderAsync(typedChars(i))
                    Dim items = state.GetCompletionItems()
                    Dim expandedCount As Integer

                    If (Not afterDot And i < ItemManager.FilterTextLengthToExcludeExpandedItemsExclusive - 1) Then
                        expandedCount = 0
                    Else
                        expandedCount = completionService.ExpandedCount
                    End If

                    Assert.True((completionService.RegularCount + expandedCount) = items.Count, $"Typed char: '{typedChars(i)}', expected: {completionService.RegularCount + expandedCount}, actual: {items.Count} ")
                    Assert.Equal(expandedCount, items.Where(Function(x) x.Flags.IsExpanded()).Count())
                Next

                ' once we show expanded items, we will not hide it in the same session, even if filter text became shorter
                For i = 1 To 4
                    state.SendBackspace()
                    Dim items = state.GetCompletionItems()
                    Assert.True((completionService.RegularCount + completionService.ExpandedCount) = items.Count, $"Backspace number: {i}expected: {completionService.RegularCount + completionService.ExpandedCount}, actual: {items.Count} ")
                    Assert.Equal(completionService.ExpandedCount, items.Where(Function(x) x.Flags.IsExpanded()).Count())
                Next
            End Using
        End Function

        <WpfFact>
        Public Async Function TestAutoHidingExpandedItemsWithDefaults() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
$$
                </Document>,
                excludedTypes:={GetType(CSharpCompletionService.Factory)}.ToList(),
                extraExportedTypes:={GetType(MockCompletionServiceFactory), GetType(MockDefaultSource)}.ToList())

                Dim workspace = state.Workspace
                workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)
                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = DirectCast(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), MockCompletionServiceFactory.Service)
                MockDefaultSource.Defaults = ImmutableArray.Create("ItemExpanded1")

                ' we blocked the expanded item task, so only regular items in the list
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Dim items = state.GetCompletionItems()
                Assert.Equal(completionService.RegularCount, items.Count)
                Assert.False(items.Any(Function(i) i.Flags.IsExpanded()))

                ' make sure expanded item task completes
                completionService.ExpandedItemsCheckpoint.Release()
                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Assert.NotNull(sessionData.ExpandedItemsTask)
                Await sessionData.ExpandedItemsTask

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                Await state.AssertSelectedCompletionItem("★ ItemExpanded1", isHardSelected:=False)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestSelectionOfPromotedDefaultItemWithEmptyFilterTextTriggeredOnArgument() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;
using System.Text;
public class Program
{
    static string Title { get; } = "";
    static void Main(string[] args)
    {
        var sb = new StringBuilder();
        sb.Append$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=True,
                extraExportedTypes:={GetType(MockDefaultSource)}.ToList())

                MockDefaultSource.Defaults = ImmutableArray.Create("Title")
                state.SendTypeChars("(")

                Dim expectedItems1 = New List(Of ValueTuple(Of String, Boolean)) From {
                    CreateExpectedItem("★ Title", isPromoted:=True),
                    CreateExpectedItem("Title", isPromoted:=False)
                }

                AssertCompletionItemsInRelativeOrder(state, expectedItems1)
                Await state.AssertSelectedCompletionItem("★ Title", isHardSelected:=False)

                state.SendTypeChars("""")
                Await state.AssertLineTextAroundCaret("        sb.Append(""", "")
            End Using
        End Function

        <ExportLanguageServiceFactory(GetType(CompletionService), LanguageNames.CSharp), [Shared], PartNotDiscoverable>
        Private Class MockCompletionServiceFactory
            Implements ILanguageServiceFactory

            Private ReadOnly _listenerProvider As IAsynchronousOperationListenerProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New(listenerProvider As IAsynchronousOperationListenerProvider)
                _listenerProvider = listenerProvider
            End Sub

            Public Function CreateLanguageService(languageServices As CodeAnalysis.Host.HostLanguageServices) As CodeAnalysis.Host.ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
                Return New Service(languageServices.LanguageServices.SolutionServices, _listenerProvider)
            End Function

            Public Class Service
                Inherits CompletionService

                Public Property RegularCount As Integer = 10
                Public Property ExpandedCount As Integer = 10

                Public ExpandedItemsCheckpoint As New Checkpoint()

                Public Sub New(services As CodeAnalysis.Host.SolutionServices, listenerProvider As IAsynchronousOperationListenerProvider)
                    MyBase.New(services, listenerProvider)
                End Sub

                Public Overrides ReadOnly Property Language As String
                    Get
                        Return LanguageNames.CSharp
                    End Get
                End Property

                Friend Overrides Function GetRules(options As CompletionOptions) As CompletionRules
                    Return CompletionRules.Default
                End Function

                Friend Overrides Async Function GetCompletionsAsync(document As Document,
                    caretPosition As Integer,
                    options As CompletionOptions,
                    passThroughOptions As OptionSet,
                    Optional trigger As CompletionTrigger = Nothing,
                    Optional roles As ImmutableHashSet(Of String) = Nothing,
                    Optional cancellationToken As CancellationToken = Nothing) As Task(Of CompletionList)

                    Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
                    Dim defaultItemSpan = GetDefaultCompletionListSpan(text, caretPosition)

                    Dim builder = ArrayBuilder(Of CompletionItem).GetInstance(RegularCount + ExpandedCount)
                    If (options.ExpandedCompletionBehavior = ExpandedCompletionMode.AllItems) Then
                        CreateRegularItems(builder, RegularCount)
                        Await CreateExpandedItems(builder, ExpandedCount)
                    ElseIf (options.ExpandedCompletionBehavior = ExpandedCompletionMode.ExpandedItemsOnly) Then
                        Await CreateExpandedItems(builder, ExpandedCount)
                    Else
                        CreateRegularItems(builder, RegularCount)
                    End If

                    Return CompletionList.Create(defaultItemSpan, builder.ToImmutableAndFree())
                End Function

                Friend Overrides Function ShouldTriggerCompletion(project As Project,
                    languageServices As CodeAnalysis.Host.LanguageServices,
                    text As SourceText,
                    caretPosition As Integer,
                    trigger As CompletionTrigger,
                    options As CompletionOptions,
                    passThroughOptions As OptionSet,
                    Optional roles As ImmutableHashSet(Of String) = Nothing) As Boolean
                    Return True
                End Function

                Private Async Function CreateExpandedItems(builder As ArrayBuilder(Of CompletionItem), count As Integer) As Task
                    Await ExpandedItemsCheckpoint.Task
                    For i = 1 To count
                        Dim item = ImportCompletionItem.Create(
                        $"ItemExpanded{i}",
                        arity:=0,
                        containingNamespace:="NS",
                        glyph:=Glyph.ClassPublic,
                        genericTypeSuffix:=String.Empty,
                        flags:=CompletionItemFlags.Expanded,
                        extensionMethodData:=Nothing,
                        includedInTargetTypeCompletion:=True)
                        builder.Add(item)
                    Next
                End Function

                Private Shared Sub CreateRegularItems(builder As ArrayBuilder(Of CompletionItem), count As Integer)
                    For i = 1 To count
                        builder.Add(CompletionItem.Create($"ItemRegular{i}"))
                    Next
                End Sub
            End Class
        End Class
    End Class
End Namespace
