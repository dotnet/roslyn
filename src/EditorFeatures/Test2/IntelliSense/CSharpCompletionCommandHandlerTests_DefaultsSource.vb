' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_DefaultsSource

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoItemMatchesDefaults(isAggressive As Boolean) As Task
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

                If isAggressive Then
                    state.TextView.Options.SetOptionValue(ItemManager.AggressiveDefaultsMatchingOptionName, True)
                End If

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsDoNotContainAny("MyA", "MyB")
                Await state.AssertSelectedCompletionItem("MyMethod", isHardSelected:=True)

            End Using
        End Function

        <WpfFact, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectFirstMatchingDefaultIfNoFilterText() As Task
            Using state = CreateTestState(
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
                Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=False) ' Not hard-selected since filter text is empty
            End Using
        End Function

        <WpfFact, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectFirstMatchingDefaultWithFilterText() As Task
            Using state = CreateTestState(
                              <Document>
using NS1;
class C
{
    void Method()
    {
        My$$
    }
}
                              </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAggressiveDefaultsMatching(isAggressive As Boolean) As Task
            Using state = CreateTestState(
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
                If isAggressive Then
                    state.TextView.Options.SetOptionValue(ItemManager.AggressiveDefaultsMatchingOptionName, True)
                End If

                state.SendInvokeCompletionList()

                If isAggressive Then
                    Await state.AssertCompletionItemsContain("My", displayTextSuffix:="")
                    Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                    Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=True)
                Else
                    Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                    Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                    Await state.AssertSelectedCompletionItem("My", isHardSelected:=True)
                End If
            End Using
        End Function

        <WpfFact, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectFirstMatchingDefaultOverCaseSensitiveEquallyGoodMatch1() As Task
            Using state = CreateTestState(
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
                Await state.AssertCompletionItemsContain("my", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("MyAB", isHardSelected:=True)

            End Using
        End Function

        <WpfFact, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectFirstMatchingDefaultOverCaseSensitiveEquallyGoodMatch2() As Task
            Using state = CreateTestState(
                              <Document>
using NS1;
class C
{
    void Method(int myA)
    {
        myA$$
    }
}
                              </Document>)

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("myA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("MyA", isHardSelected:=True)

            End Using
        End Function

        <WpfFact, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotChangeSelectionIfBetterMatch() As Task
            Using state = CreateTestState(
                              <Document>
using NS1;
class C
{
    void Method(int my)
    {
        my$$
    }
}
                              </Document>)

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                Await state.AssertSelectedCompletionItem("my", isHardSelected:=True)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotChangeIfPreselection(isAggressive As Boolean) As Task
            Using state = CreateTestState(
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
                If isAggressive Then
                    state.TextView.Options.SetOptionValue(ItemManager.AggressiveDefaultsMatchingOptionName, True)
                End If

                state.SendInvokeCompletionList()

                Await state.AssertCompletionItemsContain("MyA", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("MyAB", displayTextSuffix:="")
                ' "C" is an item with preselect priority
                Await state.AssertSelectedCompletionItem("C", isHardSelected:=True)
            End Using
        End Function

        Private Shared Function CreateTestState(documentElement As XElement) As TestState
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

            <ComponentModel.Composition.ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetSessionDefaultsAsync(session As IAsyncCompletionSession) As Task(Of ImmutableArray(Of String)) Implements IAsyncCompletionDefaultsSource.GetSessionDefaultsAsync
                Return Task.FromResult(ImmutableArray.Create("MyAB", "MyA"))
            End Function
        End Class
    End Class
End Namespace
