' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    Public Class RenameTagProducerTests
        Private Async Function VerifyEmptyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService) As Task
            Await VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, SpecializedCollections.EmptyEnumerable(Of Span))
        End Function

        Private Async Function VerifyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService) As Task
            Dim expectedSpans = actualWorkspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Select(Function(ts) ts.ToSpan())
            Await VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Function

        Private Async Function VerifyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedTaggedWorkspace As TestWorkspace) As Task
            Dim expectedSpans = expectedTaggedWorkspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Select(Function(ts) ts.ToSpan())
            Await VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Function

        Private Async Function VerifyAnnotatedTaggedSpans(tagType As TextMarkerTag, annotationString As String, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedTaggedWorkspace As TestWorkspace) As Task
            Dim annotatedDocument = expectedTaggedWorkspace.Documents.SingleOrDefault(Function(d) d.AnnotatedSpans.Any())

            Dim expectedSpans As IEnumerable(Of Span)
            If annotatedDocument Is Nothing Then
                expectedSpans = SpecializedCollections.EmptyEnumerable(Of Span)
            Else
                expectedSpans = GetAnnotatedSpans(annotationString, annotatedDocument)
            End If

            Await VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Function

        Private Shared Function GetAnnotatedSpans(annotationString As String, annotatedDocument As TestHostDocument) As IEnumerable(Of Span)
            Return annotatedDocument.AnnotatedSpans.SelectMany(Function(kvp)
                                                                   If kvp.Key = annotationString Then
                                                                       Return kvp.Value.Select(Function(ts) ts.ToSpan())
                                                                   End If
                                                                   Return SpecializedCollections.EmptyEnumerable(Of Span)
                                                               End Function)
        End Function

        Private Async Function VerifySpansBeforeConflictResolution(actualWorkspace As TestWorkspace, renameService As InlineRenameService) As Task
            ' Verify no fixup/resolved non-reference conflict span.
            Await VerifyEmptyTaggedSpans(HighlightTags.RenameFixupTag.Instance, actualWorkspace, renameService)

            ' Verify valid reference tags.
            Await VerifyTaggedSpans(HighlightTags.RenameFieldBackgroundAndBorderTag.Instance, actualWorkspace, renameService)
        End Function

        Private Async Function VerifySpansAndBufferForConflictResolution(actualWorkspace As TestWorkspace, renameService As InlineRenameService, resolvedConflictWorkspace As TestWorkspace,
                                                                session As IInlineRenameSession, Optional sessionCommit As Boolean = False, Optional sessionCancel As Boolean = False) As System.Threading.Tasks.Task
            Await WaitForRename(actualWorkspace)

            ' Verify fixup/resolved conflict spans.
            Await VerifyAnnotatedTaggedSpans(HighlightTags.RenameFixupTag.Instance, "Complexified", actualWorkspace, renameService, resolvedConflictWorkspace)

            ' Verify valid reference tags.
            Await VerifyTaggedSpans(HighlightTags.RenameFieldBackgroundAndBorderTag.Instance, actualWorkspace, renameService, resolvedConflictWorkspace)

            VerifyBufferContentsInWorkspace(actualWorkspace, resolvedConflictWorkspace)

            If sessionCommit Or sessionCancel Then
                Assert.True(Not sessionCommit Or Not sessionCancel)

                If sessionCancel Then
                    session.Cancel()
                    VerifyBufferContentsInWorkspace(actualWorkspace, actualWorkspace)
                ElseIf sessionCommit Then
                    session.Commit()
                    VerifyBufferContentsInWorkspace(actualWorkspace, resolvedConflictWorkspace)
                End If
            End If
        End Function

        Private Async Function VerifyTaggedSpansCore(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedSpans As IEnumerable(Of Span)) As Task
            Dim taggedSpans = Await GetTagsOfType(tagType, actualWorkspace, renameService)
            Assert.Equal(expectedSpans, taggedSpans)
        End Function

        Private Sub VerifyBufferContentsInWorkspace(actualWorkspace As TestWorkspace, expectedWorkspace As TestWorkspace)
            Dim actualDocs = actualWorkspace.Documents
            Dim expectedDocs = expectedWorkspace.Documents
            Assert.Equal(expectedDocs.Count, actualDocs.Count)

            For i = 0 To actualDocs.Count - 1
                Dim actualDocument = actualDocs(i)
                Dim expectedDocument = expectedDocs(i)
                Dim actualText = actualDocument.TextBuffer.CurrentSnapshot.GetText().Trim()
                Dim expectedText = expectedDocument.TextBuffer.CurrentSnapshot.GetText().Trim()
                Assert.Equal(expectedText, actualText)
            Next
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ValidTagsDuringSimpleRename() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class [|$$Goo|]
                                {
                                    void Blah()
                                    {
                                        [|Goo|] f = new [|Goo|]();
                                    }
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)

                Await VerifyTaggedSpans(HighlightTags.RenameFieldBackgroundAndBorderTag.Instance, workspace, renameService)
                session.Cancel()
            End Using
        End Function

        <WpfFact>
        <WorkItem(922197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/922197")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function UnresolvableConflictInModifiedDocument() As System.Threading.Tasks.Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
class Program
{
    static void Main(string[] {|conflict:args|}, int $$goo)
    {
        Goo(c => IsInt({|conflict:goo|}, c));
    }

    private static void Goo(Func&lt;char, bool> p) { }
    private static void Goo(Func&lt;int, bool> p) { }
    private static bool IsInt(int goo, char c) { }
}
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim document = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim location = document.CursorPosition.Value
                Dim session = StartSession(workspace)

                document.TextBuffer.Replace(New Span(location, 3), "args")
                Await WaitForRename(workspace)

                Using renamedWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] {|conflict:args|}, int args)
    {
        Goo(c => IsInt({|conflict:args|}, c));
    }

    private static void Goo(Func&lt;char, bool> p) { }
    private static void Goo(Func&lt;int, bool> p) { }
    private static bool IsInt(int goo, char c) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Dim renamedDocument = renamedWorkspace.Documents.Single()
                    Dim expectedSpans = GetAnnotatedSpans("conflict", renamedDocument)
                    Dim taggedSpans = GetTagsOfType(RenameConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_InterleavedResolvedConflicts() As System.Threading.Tasks.Task
            Using workspace = CreateWorkspaceWithWaiter(
                         <Workspace>
                             <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                                 <Document FilePath="C.cs">
public class Class1
{
#if Proj2
    int fieldclash;
#endif

    int field$$;

    void M()
    {
        int fieldclash = 8;

#if Proj1
        var a = [|field|];
#elif Proj2
        var a = [|field|];
#elif Proj3
        var a = field;
#endif


#if Proj1
        var b = [|field|];
#elif Proj2
        var b = [|field|];
#elif Proj3
        var b = field;
#endif
    }
}
                                 </Document>
                             </Project>
                             <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                             </Project>
                         </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim document = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim location = document.CursorPosition.Value
                Dim session = StartSession(workspace)

                document.TextBuffer.Insert(location, "clash")
                Await WaitForRename(workspace)

                Using renamedWorkspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                                <Document FilePath="C.cs">
public class Class1
{
#if Proj2
    int fieldclash;
#endif

    int {|valid:fieldclash|};

    void M()
    {
        int fieldclash = 8;

#if Proj1
        {|resolved:var a = this.{|valid:fieldclash|};|}
#elif Proj2
        var a = {|conflict:fieldclash|};
#elif Proj3
        var a = field;
#endif


#if Proj1
        {|resolved:var b = this.{|valid:fieldclash|};|}
#elif Proj2
        var b = {|conflict:fieldclash|};
#elif Proj3
        var b = field;
#endif
    }
}
                                </Document>
                            </Project>
                            <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                                <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                            </Project>
                        </Workspace>
                )

                    Dim renamedDocument = renamedWorkspace.Documents.First()
                    Dim expectedSpans = GetAnnotatedSpans("resolved", renamedDocument)
                    Dim taggedSpans = GetTagsOfType(RenameFixupTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)

                    expectedSpans = GetAnnotatedSpans("conflict", renamedDocument)
                    taggedSpans = GetTagsOfType(RenameConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)

                    expectedSpans = GetAnnotatedSpans("valid", renamedDocument)
                    taggedSpans = GetTagsOfType(RenameFieldBackgroundAndBorderTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_UnresolvableConflictComments() As Task
            Dim originalDocument = "
public class Class1
{
#if Proj1
    void Test(double x) { }
#elif Proj2
    void Test(long x) { }
#endif
    void Tes$$(int i) { }
    void M()
    {
        Test(5);
    }
}"
            Using workspace = CreateWorkspaceWithWaiter(
                         <Workspace>
                             <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                                 <Document FilePath="C.cs"><%= originalDocument %></Document>
                             </Project>
                             <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                             </Project>
                         </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim document = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim location = document.CursorPosition.Value
                Dim session = StartSession(workspace)

                document.TextBuffer.Insert(location, "t")
                Await WaitForRename(workspace)

                Dim expectedDocument = $"
public class Class1
{{
#if Proj1
    void Test(double x) {{ }}
#elif Proj2
    void Test(long x) {{ }}
#endif
    void Test(int i) {{ }}
    void M()
    {{
{{|conflict:{{|conflict:/* {String.Format(WorkspacesResources.Unmerged_change_from_project_0, "CSharpAssembly1")}
{WorkspacesResources.Before_colon}
        Test(5);
{WorkspacesResources.After_colon}
        Test((long)5);
*|}}|}}/
        Test((double)5);
    }}
}}"
                Using renamedWorkspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                                <Document FilePath="C.cs"><%= expectedDocument %></Document>
                            </Project>
                            <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                                <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                            </Project>
                        </Workspace>
                )

                    Dim renamedDocument = renamedWorkspace.Documents.First()
                    Dim expectedSpans = GetAnnotatedSpans("conflict", renamedDocument)
                    Dim taggedSpans = GetTagsOfType(RenameConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Function

        <WpfFact>
        <WorkItem(922197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/922197")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function UnresolvableConflictInUnmodifiedDocument() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                    class [|$$A|]
                                    {
                                  
                                    }
                                </Document>
                                <Document FilePath="B.cs">
                                    class {|conflict:B|}
                                    {
                                    }
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).TextBuffer
                Dim session = StartSession(workspace)

                textBuffer.Replace(New Span(location, 1), "B")
                Await WaitForRename(workspace)

                Dim conflictDocument = workspace.Documents.Single(Function(d) d.FilePath = "B.cs")
                Dim expectedSpans = GetAnnotatedSpans("conflict", conflictDocument)
                Dim taggedSpans = GetTagsOfType(RenameConflictTag.Instance, renameService, conflictDocument.TextBuffer)
                Assert.Equal(expectedSpans, taggedSpans)
            End Using
        End Function

        <WpfFact>
        <WorkItem(847467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847467")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ValidStateWithEmptyReplacementTextAfterConflictResolution() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class [|$$T|]
                                {
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim session = StartSession(workspace)

                textBuffer.Replace(New Span(location, 1), "this")

                ' Verify @ escaping 
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class @[|this|]
                                {
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                textBuffer.Delete(New Span(location + 1, 4))
                Await WaitForRename(workspace)

                ' Verify no escaping 
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class 
                                {
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                    VerifyBufferContentsInWorkspace(workspace, resolvedConflictWorkspace)
                End Using

                session.Commit()
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class T
                                {
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                    VerifyBufferContentsInWorkspace(workspace, resolvedConflictWorkspace)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(812789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/812789")>
        Public Async Function RenamingEscapedIdentifiers() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
class C
{
    void @$$as() { }
}
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                ' Verify @ escaping is still present
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void @[|as|]() { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                textBuffer.Replace(New Span(location, 2), "bar")

                ' Verify @ escaping is removed
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|bar|]() { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using
            End Using
        End Function

        <WpfFact>
        <WorkItem(812795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/812795")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function BackspacingAfterConflictResolutionPreservesTrackingSpans() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    void Method()
    {
        $$Bar(0);
    }
    void Goo(int i) { }
    void Bar(double d) { }
}

                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    workspace.GetService(Of IThreadingContext)(),
                    workspace.GetService(Of InlineRenameService))

                Dim session = StartSession(workspace)
                textBuffer.Replace(New Span(location, 3), "Goo")

                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void Method()
    {
        {|Complexified:[|Goo|]((double)0);|}
    }
    void Goo(int i) { }
    void [|Goo|](double d) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Delete Goo and type "as"
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace(), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace(), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace(), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "a"c), Sub() editorOperations.InsertText("a"), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "s"c), Sub() editorOperations.InsertText("s"), Utilities.TestCommandExecutionContext.Create())

                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void Method()
    {
        @[|as|](0);
    }
    void Goo(int i) { }
    void @[|as|](double d) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_NonReferenceConflict() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Goo
                                {
                                    int bar;
                                    void M(int [|$$goo|])
                                    {
                                        var x = [|goo|];
                                        bar = 23;
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved non-reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved non-reference conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Goo
                                {
                                    int bar;
                                    void M(int [|bar|])
                                    {
                                        var x = [|bar|];
                                        {|Complexified:this.{|Resolved:bar|} = 23;|}
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                textBuffer.Replace(New Span(location, 3), "baR")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Goo
                                {
                                    int bar;
                                    void M(int [|$$baR|])
                                    {
                                        var x = [|baR|];
                                        bar = 23;
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VisualBasic_FixupSpanDuringResolvableConflict_NonReferenceConflict() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Goo
                                    Dim bar As Integer
                                    Sub M([|$$goo|] As Integer)
                                        Dim x = [|goo|]
                                        BAR = 23
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved non-reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved non-reference conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Goo
                                    Dim bar As Integer
                                    Sub M([|bar|] As Integer)
                                        Dim x = [|bar|]
                                        {|Complexified:Me.{|Resolved:BAR|} = 23|}
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                textBuffer.Replace(New Span(location, 3), "boo")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Goo
                                    Dim bar As Integer
                                    Sub M([|$$boo|] As Integer)
                                        Dim x = [|boo|]
                                        BAR = 23
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_ReferenceConflict() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int [|$$goo|];
    void M(int bar)
    {
        [|goo|] = [|goo|] + bar;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int [|bar|];
    void M(int bar)
    {
        {|Complexified:this.{|Resolved:[|bar|]|} = this.{|Resolved:[|bar|]|} + bar;|}
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                textBuffer.Replace(New Span(location, 3), "ba")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int [|$$ba|];
    void M(int bar)
    {
        [|ba|] = [|ba|] + bar;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCancel:=True)
                End Using
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/38247")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VisualBasic_FixupSpanDuringResolvableConflict_ReferenceConflict() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [|$$goo|] As Integer
    Sub M(bar As Integer)
        [|goo|] = [|goo|] + bar
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [|bar|] As Integer
    Sub M(bar As Integer)
        {|Complexified:Me.{|Resolved:[|bar|]|} = Me.{|Resolved:[|bar|]|} + bar|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                textBuffer.Replace(New Span(location, 3), "ba")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [|$$ba|] As Integer
    Sub M(bar As Integer)
        [|ba|] = [|ba|] + bar
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCancel:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_NeedsEscaping() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int @int;
    void M(int [|$$goo|])
    {
        var x = [|goo|];
        @int = 23;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved escaping conflict.
                textBuffer.Replace(New Span(location, 3), "int")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int @int;
    void M(int {|Resolved:@[|int|]|})
    {
        var x = {|Resolved:@[|int|]|};
        {|Complexified:this.{|Resolved:@int|} = 23;|}
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit to change "int" to "@in" so that we have no more conflicts, just escaping.
                textBuffer.Replace(New Span(location + 1, 3), "in")

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Goo
{
    int @int;
    void M(int {|Resolved:@[|in|]|})
    {
        var x = {|Resolved:@[|in|]|};
        @int = 23;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VisualBasic_FixupSpanDuringResolvableConflict_NeedsEscaping() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [New] As Integer
    Sub M([|$$goo|] As Integer)
        Dim x = [|goo|]
        [NEW] = 23
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved escaping conflict.
                textBuffer.Replace(New Span(location, 3), "New")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [New] As Integer
    Sub M({|Resolved:[[|New|]]|} As Integer)
        Dim x = {|Resolved:[[|New|]]|}
        {|Complexified:Me.{|Resolved:[NEW]|} = 23|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit to change "New" to "[Do]" so that we have no more conflicts, just escaping.
                textBuffer.Replace(New Span(location + 1, 3), "Do")

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Dim [New] As Integer
    Sub M({|Resolved:[[|Do|]]|} As Integer)
        Dim x = {|Resolved:[[|Do|]]|}
        [NEW] = 23
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function FixupSpanDuringResolvableConflict_VerifyCaret() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Sub [|N$$w|](goo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    workspace.GetService(Of IThreadingContext)(),
                    workspace.GetService(Of InlineRenameService))

                Dim textViewService = New TextBufferAssociatedViewService()
                Dim buffers = New Collection(Of ITextBuffer)
                buffers.Add(view.TextBuffer)
                DirectCast(textViewService, ITextViewConnectionListener).SubjectBuffersConnected(view, ConnectionReason.TextViewLifetime, buffers)

                Dim renameService = workspace.GetService(Of InlineRenameService)()

                Dim session = StartSession(workspace)
                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "e"c),
                                              Sub() editorOperations.InsertText("e"),
                                              Utilities.TestCommandExecutionContext.Create())

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Sub [[|Ne$$w|]](goo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                    Dim location = view.Caret.Position.BufferPosition.Position
                    Dim expectedLocation = resolvedConflictWorkspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                    Assert.Equal(expectedLocation, location)
                End Using

                ' Make another edit to change "New" to "Nexw" so that we have no more conflicts or escaping.
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "x"c),
                                              Sub() editorOperations.InsertText("x"),
                                              Utilities.TestCommandExecutionContext.Create())

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Sub [|Nex$$w|](goo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                    Dim location = view.Caret.Position.BufferPosition.Position
                    Dim expectedLocation = resolvedConflictWorkspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                    Assert.Equal(expectedLocation, location)
                End Using
            End Using
        End Function

        <WpfFact>
        <WorkItem(771743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771743")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyNoSelectionAfterCommit() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Goo
    Sub [|M$$ain|](goo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    workspace.GetService(Of IThreadingContext)(),
                    workspace.GetService(Of InlineRenameService))

                Dim textViewService = New TextBufferAssociatedViewService()
                Dim buffers = New Collection(Of ITextBuffer)
                buffers.Add(view.TextBuffer)
                DirectCast(textViewService, ITextViewConnectionListener).SubjectBuffersConnected(view, ConnectionReason.TextViewLifetime, buffers)

                Dim location = view.Caret.Position.BufferPosition.Position
                view.Selection.Select(New SnapshotSpan(view.Caret.Position.BufferPosition, 2), False)
                Dim renameService = workspace.GetService(Of InlineRenameService)()

                Dim session = StartSession(workspace)
                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Type few characters.
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "e"c), Sub() editorOperations.InsertText("e"), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "f"c), Sub() editorOperations.InsertText("f"), Utilities.TestCommandExecutionContext.Create())
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "g"c), Sub() editorOperations.InsertText("g"), Utilities.TestCommandExecutionContext.Create())

                session.Commit()
                Dim selectionLength = view.Selection.End.Position - view.Selection.Start.Position
                Assert.Equal(0, selectionLength)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_ComplexificationOutsideConflict() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        int x = [|$$Bar|](Goo([|Bar|](0)));
    }

    static int Goo(int i)
    {
        return 0;
    }

    static int [|Bar|](double d)
    {
        return 1;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "Goo")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        {|Complexified:int x = {|Resolved:[|Goo|]|}((double)Goo({|Resolved:[|Goo|]|}((double)0)));|}
    }

    static int Goo(int i)
    {
        return 0;
    }

    static int [|Goo|](double d)
    {
        return 1;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                textBuffer.Replace(New Span(location, 3), "GOO")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        int x = [|$$GOO|](Goo([|GOO|](0)));
    }

    static int Goo(int i)
    {
        return 0;
    }

    static int [|GOO|](double d)
    {
        return 1;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_ContainedComplexifiedSpan() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;
namespace N
{
    class A<T>
    {
        public virtual void Goo(T x) { }
        class B<S> : A<B<S>>
        {
            class [|$$C|]<U> : B<[|C|]<U>> // Rename C to A
            {
                public override void Goo(A<A<T>.B<S>>.B<A<T>.B<S>.[|C|]<U>> x) { }
            }
        }
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 1), "A")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;
namespace N
{
    class A<T>
    {
        public virtual void Goo(T x) { }
        class B<S> : A<B<S>>
        {
            class [|A|]<U> : B<[|A|]<U>> // Rename C to A
            {
                public override void Goo({|Complexified:N.{|Resolved:A|}<N.{|Resolved:A|}<T>.B<S>>|}.B<{|Complexified:N.{|Resolved:A|}<T>|}.B<S>.[|A|]<U>> x) { }
            }
        }
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(8334, "https://github.com/dotnet/roslyn/issues/8334")>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_ComplexificationReordersReferenceSpans() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
static class E
{
    public static C [|$$Goo|](this C x, int tag) { return new C(); }
}
                            
class C
{
    C Bar(int tag)
    {
        return this.[|Goo|](1).[|Goo|](2);
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "Bar")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
static class E
{
    public static C [|Bar|](this C x, int tag) { return new C(); }
}
                            
class C
{
    C Bar(int tag)
    {
        {|Complexified:return E.{|Resolved:[|Bar|]|}(E.{|Resolved:[|Bar|]|}(this, 1), 2);|}
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_WithinCrefs() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;
using F = N;
namespace N
{
    interface I
    {
        void Goo();
    }
}

class C
{
    class E : F.I
    {
        /// <summary>
        /// This is a function <see cref="F.I.Goo"/>
        /// </summary>
        public void Goo() { }
    }

    class [|$$K|]
    {
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 1), "F")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;
using F = N;
namespace N
{
    interface I
    {
        void Goo();
    }
}

class C
{
    class E : {|Complexified:{|Resolved:N|}|}.I
    {
        /// <summary>
        /// This is a function <see cref="{|Complexified:{|Resolved:N|}|}.I.Goo"/>
        /// </summary>
        public void Goo() { }
    }

    class [|$$F|]
    {
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharp_FixupSpanDuringResolvableConflict_OverLoadResolutionChangesInEnclosingInvocations() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Main()
    {
        Outer(y => Inner(x => x.Ex(), y), 0);
    }
}

static class E
{
    public static void [|$$Ex|](this int x) { } // Rename Ex to Goo
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                Await VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 2), "Goo")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
using System;

static class C
{
    static void Ex(this string x) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Main()
    {
        {|Complexified:{|Resolved:Outer|}((string y) => {|Resolved:Inner|}(x => x.Ex(), y), 0);|}
    }
}

static class E
{
    public static void [|Goo|](this int x) { } // Rename Ex to Goo
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    Await VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Function

        <WpfFact>
        <WorkItem(530817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530817")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CSharpShowDeclarationConflictsImmediately() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class Program
                                {    
                                  static void Main(string[] args)    
                                  {        
                                    const int {|valid:$$V|} = 5;
                                    int {|conflict:V|} = 99;        
                                  }
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)

                Dim validTaggedSpans = Await GetTagsOfType(HighlightTags.RenameFieldBackgroundAndBorderTag.Instance, workspace, renameService)
                Dim validExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("valid").Select(Function(ts) ts.ToSpan())

                Dim conflictTaggedSpans = Await GetTagsOfType(RenameConflictTag.Instance, workspace, renameService)
                Dim conflictExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("conflict").Select(Function(ts) ts.ToSpan())

                session.Cancel()

                AssertEx.Equal(validExpectedSpans, validTaggedSpans)
                AssertEx.Equal(conflictExpectedSpans, conflictTaggedSpans)
            End Using
        End Function

        <WpfFact>
        <WorkItem(530817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530817")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VBShowDeclarationConflictsImmediately() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
                                Class Goo
                                    Sub Bar()
                                        Dim {|valid:$$V|} as Integer
                                        Dim {|conflict:V|} as String
                                    End Sub
                                End Class
                            </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)

                Dim validTaggedSpans = Await GetTagsOfType(HighlightTags.RenameFieldBackgroundAndBorderTag.Instance, workspace, renameService)
                Dim validExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("valid").Select(Function(ts) ts.ToSpan())

                Dim conflictTaggedSpans = Await GetTagsOfType(RenameConflictTag.Instance, workspace, renameService)
                Dim conflictExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("conflict").Select(Function(ts) ts.ToSpan())

                session.Cancel()

                AssertEx.Equal(validExpectedSpans, validTaggedSpans)
                AssertEx.Equal(conflictExpectedSpans, conflictTaggedSpans)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ActiveSpanInSecondaryView() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
                                    Class [|$$Goo|]
                                    End Class
                                </Document>
                                <Document>
                                    ' [|Goo|]
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents(0).TextBuffer
                Dim session = StartSession(workspace)
                session.RefreshRenameSessionWithOptionsChanged(CodeAnalysis.Rename.RenameOptions.RenameInComments, newValue:=True)
                Await WaitForRename(workspace)

                session.RefreshRenameSessionWithOptionsChanged(CodeAnalysis.Rename.RenameOptions.RenameInComments, newValue:=False)
                Await WaitForRename(workspace)

                textBuffer.Replace(New Span(location, 3), "Bar")
                Await WaitForRename(workspace)
            End Using
        End Function

        Private Async Function GetTagsOfType(expectedTagType As ITextMarkerTag, workspace As TestWorkspace, renameService As InlineRenameService) As Task(Of IEnumerable(Of Span))
            Dim textBuffer = workspace.Documents.Single().TextBuffer
            Await WaitForRename(workspace)

            Return GetTagsOfType(expectedTagType, renameService, textBuffer)
        End Function

        Private Shared Function GetTagsOfType(expectedTagType As ITextMarkerTag, renameService As InlineRenameService, textBuffer As ITextBuffer) As IEnumerable(Of Span)
            Dim tagger = New RenameTagger(textBuffer, renameService)
            Dim tags = tagger.GetTags(textBuffer.CurrentSnapshot.GetSnapshotSpanCollection())

            Return (From tag In tags
                    Where tag.Tag Is expectedTagType
                    Order By tag.Span.Start
                    Select tag.Span.Span).ToList()
        End Function
    End Class
End Namespace
