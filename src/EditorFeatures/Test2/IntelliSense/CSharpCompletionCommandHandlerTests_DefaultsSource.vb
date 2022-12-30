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

        <WpfFact>
        Public Async Function TestNoItemMatchesDefaults() As Task
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

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsDoNotContainAny("MyAB", "MyA")
                Await state.AssertSelectedCompletionItem("MyMethod", isHardSelected:=True)

            End Using
        End Function

        <WpfFact, CombinatorialData>
        <WorkItem(61120, "https://github.com/dotnet/roslyn/issues/61120")>
        Public Async Function SelectFirstMatchingDefaultIfNoFilterText() As Task
            Using state = CreateTestStateWithAdditionalDocument(
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

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=True) ' hard-selected since filter text is empty
            End Using
        End Function

        <WpfFact, CombinatorialData>
        Public Async Function SelectFirstMatchingDefaultWithPrefixFilterText() As Task
            Using state = CreateTestStateWithAdditionalDocument(
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
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyMethod", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=True)
            End Using
        End Function

        <WpfFact>
        Public Async Function DoNotChangeSelectionIfBetterMatch_CaseSensivePrefix() As Task
            Using state = CreateTestStateWithAdditionalDocument(
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

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("my", isHardSelected:=True)

            End Using
        End Function

        <WpfFact>
        Public Async Function DoNotChangeSelectionIfBetterMatch_ExactOverPrefix() As Task
            Using state = CreateTestStateWithAdditionalDocument(
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

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("My", isHardSelected:=True)

            End Using
        End Function

        <WpfFact, CombinatorialData>
        Public Async Function DoNotChangeIfPreselection() As Task
            Using state = CreateTestStateWithAdditionalDocument(
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

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                ' "C" is an item with preselect priority
                Await state.AssertSelectedCompletionItem("C", isHardSelected:=True)
            End Using
        End Function

        Private Shared Function CreateTestStateWithAdditionalDocument(documentElement As XElement) As TestState
            MockDefaultSource.Defaults = ImmutableArray.Create("MyAB", "MyA")
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
        Public Async Function SelectDefaultItemOverStarredItem() As Task
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

                Await state.AssertCompletionItemsContainAll("★ FirstStarred", "★ SecondStarred", "FirstStarred", "SecondStarred", "FirstDefault")
                Await state.AssertSelectedCompletionItem("FirstDefault", isHardSelected:=True)

                state.SendTypeChars("First")
                Await state.AssertCompletionItemsContainAll("★ FirstStarred", "FirstStarred", "FirstDefault")
                Await state.AssertSelectedCompletionItem("FirstDefault", isHardSelected:=True)

                state.SendTypeChars("S")
                Await state.AssertSelectedCompletionItem("★ FirstStarred", isHardSelected:=True)
            End Using
        End Function

        <WpfFact>
        Public Async Function SelectStarredItemInDefaultList() As Task
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

                Await state.AssertCompletionItemsContainAll("★ FirstStarred", "★ SecondStarred", "FirstStarred", "SecondStarred", "FirstDefault")
                Await state.AssertSelectedCompletionItem("★ SecondStarred", isHardSelected:=True)

                state.SendTypeChars("starred")
                Await state.AssertCompletionItemsContainAll("★ FirstStarred", "FirstStarred", "★ SecondStarred", "SecondStarred")
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
