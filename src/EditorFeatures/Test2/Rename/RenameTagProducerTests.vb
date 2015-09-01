' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags
Imports Microsoft.CodeAnalysis.Editor.Shared.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Public Class RenameTagProducerTests
        Private Sub VerifyEmptyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService)
            VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, SpecializedCollections.EmptyEnumerable(Of Span))
        End Sub

        Private Sub VerifyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService)
            Dim expectedSpans = actualWorkspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Select(Function(ts) ts.ToSpan())
            VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Sub

        Private Sub VerifyTaggedSpans(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedTaggedWorkspace As TestWorkspace)
            Dim expectedSpans = expectedTaggedWorkspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Select(Function(ts) ts.ToSpan())
            VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Sub

        Private Sub VerifyAnnotatedTaggedSpans(tagType As TextMarkerTag, annotationString As String, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedTaggedWorkspace As TestWorkspace)
            Dim annotatedDocument = expectedTaggedWorkspace.Documents.SingleOrDefault(Function(d) d.AnnotatedSpans.Any())

            Dim expectedSpans As IEnumerable(Of Span)
            If annotatedDocument Is Nothing Then
                expectedSpans = SpecializedCollections.EmptyEnumerable(Of Span)
            Else
                expectedSpans = GetAnnotatedSpans(annotationString, annotatedDocument)
            End If

            VerifyTaggedSpansCore(tagType, actualWorkspace, renameService, expectedSpans)
        End Sub

        Private Shared Function GetAnnotatedSpans(annotationString As String, annotatedDocument As TestHostDocument) As IEnumerable(Of Span)
            Return annotatedDocument.AnnotatedSpans.SelectMany(Function(kvp)
                                                                   If kvp.Key = annotationString Then
                                                                       Return kvp.Value.Select(Function(ts) ts.ToSpan())
                                                                   End If
                                                                   Return SpecializedCollections.EmptyEnumerable(Of Span)
                                                               End Function)
        End Function

        Private Sub VerifySpansBeforeConflictResolution(actualWorkspace As TestWorkspace, renameService As InlineRenameService)
            ' Verify no fixup/resolved non-reference conflict span.
            VerifyEmptyTaggedSpans(HighlightTags.FixupTag.Instance, actualWorkspace, renameService)

            ' Verify valid reference tags.
            VerifyTaggedSpans(HighlightTags.ValidTag.Instance, actualWorkspace, renameService)
        End Sub

        Private Sub VerifySpansAndBufferForConflictResolution(actualWorkspace As TestWorkspace, renameService As InlineRenameService, resolvedConflictWorkspace As TestWorkspace,
                                                                session As IInlineRenameSession, Optional sessionCommit As Boolean = False, Optional sessionCancel As Boolean = False)
            WaitForRename(actualWorkspace)

            ' Verify fixup/resolved conflict spans.
            VerifyAnnotatedTaggedSpans(HighlightTags.FixupTag.Instance, "Complexified", actualWorkspace, renameService, resolvedConflictWorkspace)

            ' Verify valid reference tags.
            VerifyTaggedSpans(HighlightTags.ValidTag.Instance, actualWorkspace, renameService, resolvedConflictWorkspace)

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
        End Sub

        Private Sub VerifyTaggedSpansCore(tagType As TextMarkerTag, actualWorkspace As TestWorkspace, renameService As InlineRenameService, expectedSpans As IEnumerable(Of Span))
            Dim taggedSpans = GetTagsOfType(tagType, actualWorkspace, renameService)
            Assert.Equal(expectedSpans, taggedSpans)
        End Sub

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

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ValidTagsDuringSimpleRename()
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
                                class [|$$Foo|]
                                {
                                    void Blah()
                                    {
                                        [|Foo|] f = new [|Foo|]();
                                    }
                                }
                            </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)

                VerifyTaggedSpans(HighlightTags.ValidTag.Instance, workspace, renameService)
                session.Cancel()
            End Using
        End Sub

        <Fact>
        <WorkItem(922197)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub UnresolvableConflictInModifiedDocument()
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true">
                                <Document>
class Program
{
    static void Main(string[] {|conflict:args|}, int $$foo)
    {
        Foo(c => IsInt({|conflict:foo|}, c));
    }

    private static void Foo(Func&lt;char, bool> p) { }
    private static void Foo(Func&lt;int, bool> p) { }
    private static bool IsInt(int foo, char c) { }
}
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim document = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim location = document.CursorPosition.Value
                Dim session = StartSession(workspace)

                document.TextBuffer.Replace(New Span(location, 3), "args")
                WaitForRename(workspace)

                Using renamedWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] {|conflict:args|}, int args)
    {
        Foo(c => IsInt({|conflict:args|}, c));
    }

    private static void Foo(Func&lt;char, bool> p) { }
    private static void Foo(Func&lt;int, bool> p) { }
    private static bool IsInt(int foo, char c) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    Dim renamedDocument = renamedWorkspace.Documents.Single()
                    Dim expectedSpans = GetAnnotatedSpans("conflict", renamedDocument)
                    Dim taggedSpans = GetTagsOfType(ConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyLinkedFiles_InterleavedResolvedConflicts()
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
                WaitForRename(workspace)

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
                    Dim taggedSpans = GetTagsOfType(FixupTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)

                    expectedSpans = GetAnnotatedSpans("conflict", renamedDocument)
                    taggedSpans = GetTagsOfType(ConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)

                    expectedSpans = GetAnnotatedSpans("valid", renamedDocument)
                    taggedSpans = GetTagsOfType(ValidTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyLinkedFiles_UnresolvableConflictComments()
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
                WaitForRename(workspace)

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
{{|conflict:{{|conflict:/* {String.Format(WorkspacesResources.UnmergedChangeFromProject, "CSharpAssembly1")}
{WorkspacesResources.BeforeHeader}
        Test(5);
{WorkspacesResources.AfterHeader}
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
                    Dim taggedSpans = GetTagsOfType(ConflictTag.Instance, renameService, document.TextBuffer)
                    Assert.Equal(expectedSpans, taggedSpans)
                End Using
            End Using
        End Sub

        <Fact>
        <WorkItem(922197)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub UnresolvableConflictInUnmodifiedDocument()
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
                WaitForRename(workspace)

                Dim conflictDocument = workspace.Documents.Single(Function(d) d.FilePath = "B.cs")
                Dim expectedSpans = GetAnnotatedSpans("conflict", conflictDocument)
                Dim taggedSpans = GetTagsOfType(ConflictTag.Instance, renameService, conflictDocument.TextBuffer)
                Assert.Equal(expectedSpans, taggedSpans)
            End Using
        End Sub

        <Fact>
        <WorkItem(847467)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ValidStateWithEmptyReplacementTextAfterConflictResolution()
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                textBuffer.Delete(New Span(location + 1, 4))
                WaitForRename(workspace)

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
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(812789)>
        Public Sub RenamingEscapedIdentifiers()
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using
            End Using
        End Sub

        <Fact>
        <WorkItem(812795)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub BackspacingAfterConflictResolutionPreservesTrackingSpans()
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
    void Foo(int i) { }
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
                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                textBuffer.Replace(New Span(location, 3), "Foo")

                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void Method()
    {
        {|Complexified:[|Foo|]((double)0);|}
    }
    void Foo(int i) { }
    void [|Foo|](double d) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Delete Foo and type "as"
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace())
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace())
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace())
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "a"c), Sub() editorOperations.InsertText("a"))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "s"c), Sub() editorOperations.InsertText("s"))

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
    void Foo(int i) { }
    void @[|as|](double d) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_NonReferenceConflict()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Foo
                                {
                                    int bar;
                                    void M(int [|$$foo|])
                                    {
                                        var x = [|foo|];
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved non-reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved non-reference conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                textBuffer.Replace(New Span(location, 3), "baR")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VisualBasic_FixupSpanDuringResolvableConflict_NonReferenceConflict()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Foo
                                    Dim bar As Integer
                                    Sub M([|$$foo|] As Integer)
                                        Dim x = [|foo|]
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved non-reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved non-reference conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Foo
                                    Dim bar As Integer
                                    Sub M([|bar|] As Integer)
                                        Dim x = [|bar|]
                                        {|Complexified:Me.{|Resolved:BAR|} = 23|}
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                textBuffer.Replace(New Span(location, 3), "boo")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                Class Foo
                                    Dim bar As Integer
                                    Sub M([|$$boo|] As Integer)
                                        Dim x = [|boo|]
                                        BAR = 23
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_ReferenceConflict()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    int [|$$foo|];
    void M(int bar)
    {
        [|foo|] = [|foo|] + bar;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                textBuffer.Replace(New Span(location, 3), "ba")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCancel:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VisualBasic_FixupSpanDuringResolvableConflict_ReferenceConflict()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [|$$foo|] As Integer
    Sub M(bar As Integer)
        [|foo|] = [|foo|] + bar
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "bar")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [|bar|] As Integer
    Sub M(bar As Integer)
        {|Complexified:Me.{|Resolved:[|bar|]|} = Me.{|Resolved:[|bar|]|} + bar|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                textBuffer.Replace(New Span(location, 3), "ba")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [|$$ba|] As Integer
    Sub M(bar As Integer)
        [|ba|] = [|ba|] + bar
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCancel:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_NeedsEscaping()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
{
    int @int;
    void M(int [|$$foo|])
    {
        var x = [|foo|];
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved escaping conflict.
                textBuffer.Replace(New Span(location, 3), "int")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit to change "int" to "@in" so that we have no more conflicts, just escaping.
                textBuffer.Replace(New Span(location + 1, 3), "in")

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Foo
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

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VisualBasic_FixupSpanDuringResolvableConflict_NeedsEscaping()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [New] As Integer
    Sub M([|$$foo|] As Integer)
        Dim x = [|foo|]
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved escaping conflict.
                textBuffer.Replace(New Span(location, 3), "New")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [New] As Integer
    Sub M({|Resolved:[[|New|]]|} As Integer)
        Dim x = {|Resolved:[[|New|]]|}
        {|Complexified:Me.{|Resolved:[NEW]|} = 23|}
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit to change "New" to "[Do]" so that we have no more conflicts, just escaping.
                textBuffer.Replace(New Span(location + 1, 3), "Do")

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Dim [New] As Integer
    Sub M({|Resolved:[[|Do|]]|} As Integer)
        Dim x = {|Resolved:[[|Do|]]|}
        [NEW] = 23
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub FixupSpanDuringResolvableConflict_VerifyCaret()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Sub [|N$$w|](foo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim textViewService = New TextBufferAssociatedViewService()
                Dim buffers = New Collection(Of ITextBuffer)
                buffers.Add(view.TextBuffer)
                DirectCast(textViewService, IWpfTextViewConnectionListener).SubjectBuffersConnected(view, ConnectionReason.TextViewLifetime, buffers)

                Dim renameService = workspace.GetService(Of InlineRenameService)()

                Dim session = StartSession(workspace)
                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "e"c), Sub() editorOperations.InsertText("e"))

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Sub [[|Ne$$w|]](foo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                    Dim location = view.Caret.Position.BufferPosition.Position
                    Dim expectedLocation = resolvedConflictWorkspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                    Assert.Equal(expectedLocation, location)
                End Using

                ' Make another edit to change "New" to "Nexw" so that we have no more conflicts or escaping.
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "x"c), Sub() editorOperations.InsertText("x"))

                ' Verify resolved escaping conflict spans.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Sub [|Nex$$w|](foo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                    Dim location = view.Caret.Position.BufferPosition.Position
                    Dim expectedLocation = resolvedConflictWorkspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                    Assert.Equal(expectedLocation, location)
                End Using
            End Using
        End Sub

        <Fact>
        <WorkItem(771743)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyNoSelectionAfterCommit()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Class Foo
    Sub [|M$$ain|](foo As Integer)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim textViewService = New TextBufferAssociatedViewService()
                Dim buffers = New Collection(Of ITextBuffer)
                buffers.Add(view.TextBuffer)
                DirectCast(textViewService, IWpfTextViewConnectionListener).SubjectBuffersConnected(view, ConnectionReason.TextViewLifetime, buffers)

                Dim location = view.Caret.Position.BufferPosition.Position
                view.Selection.Select(New SnapshotSpan(view.Caret.Position.BufferPosition, 2), False)
                Dim renameService = workspace.GetService(Of InlineRenameService)()

                Dim session = StartSession(workspace)
                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Type few characters.
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "e"c), Sub() editorOperations.InsertText("e"))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "f"c), Sub() editorOperations.InsertText("f"))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "g"c), Sub() editorOperations.InsertText("g"))

                session.Commit()
                Dim selectionLength = view.Selection.End.Position - view.Selection.Start.Position
                Assert.Equal(0, selectionLength)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_ComplexificationOutsideConflict()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        int x = [|$$Bar|](Foo([|Bar|](0)));
    }

    static int Foo(int i)
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 3), "Foo")

                ' Verify fixup/resolved conflict span.
                Using resolvedConflictWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        {|Complexified:int x = {|Resolved:[|Foo|]|}((double)Foo({|Resolved:[|Foo|]|}((double)0)));|}
    }

    static int Foo(int i)
    {
        return 0;
    }

    static int [|Foo|](double d)
    {
        return 1;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session)
                End Using

                ' Make another edit so that we have no more conflicts.
                textBuffer.Replace(New Span(location, 3), "FOO")

                Using newWorkspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    static void Main(string[] args)
    {
        int x = [|$$FOO|](Foo([|FOO|](0)));
    }

    static int Foo(int i)
    {
        return 0;
    }

    static int [|FOO|](double d)
    {
        return 1;
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, newWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_ContainedComplexifiedSpan()
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
        public virtual void Foo(T x) { }
        class B<S> : A<B<S>>
        {
            class [|$$C|]<U> : B<[|C|]<U>> // Rename C to A
            {
                public override void Foo(A<A<T>.B<S>>.B<A<T>.B<S>.[|C|]<U>> x) { }
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

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
        public virtual void Foo(T x) { }
        class B<S> : A<B<S>>
        {
            class [|A|]<U> : B<[|A|]<U>> // Rename C to A
            {
                public override void Foo({|Complexified:N.{|Resolved:A|}<N.{|Resolved:A|}<T>.B<S>>|}.B<{|Complexified:N.{|Resolved:A|}<T>|}.B<S>.[|A|]<U>> x) { }
            }
        }
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_ComplexificationReordersReferenceSpans()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
static class E
{
    public static C [|$$Foo|](this C x, int tag) { return new C(); }
}
                            
class C
{
    C Bar(int tag)
    {
        return this.[|Foo|](1).[|Foo|](2);
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                VerifySpansBeforeConflictResolution(workspace, renameService)

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
        {|Complexified:return E.{|Resolved:[|Bar|]|}(E.{|Resolved:[|Bar|]|}(this,1),2);|}
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_WithinCrefs()
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
        void Foo();
    }
}

class C
{
    class E : F.I
    {
        /// <summary>
        /// This is a function <see cref="F.I.Foo"/>
        /// </summary>
        public void Foo() { }
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

                VerifySpansBeforeConflictResolution(workspace, renameService)

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
        void Foo();
    }
}

class C
{
    class E : {|Complexified:{|Resolved:N|}|}.I
    {
        /// <summary>
        /// This is a function <see cref="{|Complexified:{|Resolved:N|}|}.I.Foo"/>
        /// </summary>
        public void Foo() { }
    }

    class [|$$F|]
    {
    }
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharp_FixupSpanDuringResolvableConflict_OverLoadResolutionChangesInEnclosingInvocations()
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
    public static void [|$$Ex|](this int x) { } // Rename Ex to Foo
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim session = StartSession(workspace)
                Dim textBuffer = workspace.Documents.Single().TextBuffer
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

                VerifySpansBeforeConflictResolution(workspace, renameService)

                ' Apply edit so that we have a resolved reference conflict.
                textBuffer.Replace(New Span(location, 2), "Foo")

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
    public static void [|Foo|](this int x) { } // Rename Ex to Foo
}
                                ]]>
                            </Document>
                        </Project>
                    </Workspace>)

                    VerifySpansAndBufferForConflictResolution(workspace, renameService, resolvedConflictWorkspace, session, sessionCommit:=True)
                End Using
            End Using
        End Sub

        <Fact>
        <WorkItem(530817)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CSharpShowDeclarationConflictsImmediately()
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

                Dim validTaggedSpans = GetTagsOfType(HighlightTags.ValidTag.Instance, workspace, renameService)
                Dim validExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("valid").Select(Function(ts) ts.ToSpan())

                Dim conflictTaggedSpans = GetTagsOfType(ConflictTag.Instance, workspace, renameService)
                Dim conflictExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("conflict").Select(Function(ts) ts.ToSpan())

                session.Cancel()

                AssertEx.Equal(validExpectedSpans, validTaggedSpans)
                AssertEx.Equal(conflictExpectedSpans, conflictTaggedSpans)
            End Using
        End Sub

        <Fact>
        <WorkItem(530817)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VBShowDeclarationConflictsImmediately()
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
                                Class Foo
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

                Dim validTaggedSpans = GetTagsOfType(HighlightTags.ValidTag.Instance, workspace, renameService)
                Dim validExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("valid").Select(Function(ts) ts.ToSpan())

                Dim conflictTaggedSpans = GetTagsOfType(ConflictTag.Instance, workspace, renameService)
                Dim conflictExpectedSpans = workspace.Documents.Single(Function(d) d.AnnotatedSpans.Count > 0).AnnotatedSpans("conflict").Select(Function(ts) ts.ToSpan())

                session.Cancel()

                AssertEx.Equal(validExpectedSpans, validTaggedSpans)
                AssertEx.Equal(conflictExpectedSpans, conflictTaggedSpans)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ActiveSpanInSecondaryView()
            Using workspace = CreateWorkspaceWithWaiter(
                        <Workspace>
                            <Project Language="Visual Basic" CommonReferences="true">
                                <Document>
                                    Class [|$$Foo|]
                                    End Class
                                </Document>
                                <Document>
                                    ' [|Foo|]
                                </Document>
                            </Project>
                        </Workspace>)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim location = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents(0).TextBuffer
                Dim session = StartSession(workspace)
                session.RefreshRenameSessionWithOptionsChanged(CodeAnalysis.Rename.RenameOptions.RenameInComments, newValue:=True)
                WaitForRename(workspace)

                session.RefreshRenameSessionWithOptionsChanged(CodeAnalysis.Rename.RenameOptions.RenameInComments, newValue:=False)
                WaitForRename(workspace)

                textBuffer.Replace(New Span(location, 3), "Bar")
                WaitForRename(workspace)
            End Using
        End Sub

        Private Function GetTagsOfType(expectedTagType As ITextMarkerTag, workspace As TestWorkspace, renameService As InlineRenameService) As IEnumerable(Of Span)
            Dim textBuffer = workspace.Documents.Single().TextBuffer
            WaitForRename(workspace)

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
