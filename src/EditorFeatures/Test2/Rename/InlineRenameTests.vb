' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CSharp.CodeRefactorings.IntroduceVariable
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Public Class InlineRenameTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function SimpleEditAndCommit() As Task
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

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace, "BarFoo")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(540120)>
        Public Async Function SimpleEditAndVerifyTagsPropagatedAndCommit() As Task
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

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                Await WaitForRename(workspace)

                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace, "BarFoo")
            End Using
        End Function

        Private Async Function VerifyRenameOptionChangedSessionCommit(workspace As TestWorkspace,
                                                           originalTextToRename As String,
                                                           renameTextPrefix As String,
                                                           Optional renameOverloads As Boolean = False,
                                                           Optional renameInStrings As Boolean = False,
                                                           Optional renameInComments As Boolean = False) As Task
            Dim optionSet = workspace.Options
            optionSet = optionSet.WithChangedOption(RenameOptions.RenameOverloads, renameOverloads)
            optionSet = optionSet.WithChangedOption(RenameOptions.RenameInStrings, renameInStrings)
            optionSet = optionSet.WithChangedOption(RenameOptions.RenameInComments, renameInComments)

            Dim optionService = workspace.Services.GetService(Of IOptionService)()
            optionService.SetOptions(optionSet)

            Dim session = StartSession(workspace)

            ' Type a bit in the file
            Dim renameDocument As TestHostDocument = workspace.DocumentWithCursor
            renameDocument.TextBuffer.Insert(renameDocument.CursorPosition.Value, renameTextPrefix)

            Dim replacementText = renameTextPrefix + originalTextToRename
            Await WaitForRename(workspace)

            Await VerifyTagsAreCorrect(workspace, replacementText)

            session.Commit()

            Await VerifyTagsAreCorrect(workspace, replacementText)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700921)>
        Public Async Function RenameOverloadsCSharp() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    public void [|$$foo|]()
    {
        [|foo|]();
    }

    public void [|foo|]&lt;T&gt;()
    {
        [|foo|]&lt;T&gt;();
    }

    public void [|foo|](int i)
    {
        [|foo|](i);
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameOverloads:=True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700921)>
        Public Async Function RenameOverloadsVisualBasic() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Imports System.Collections.Generic
Imports System.Linq
Imports System

Public Class Program
    Sub Main(args As String())

    End Sub

    Public Sub [|$$foo|]()
        [|foo|]()
    End Sub

    Public Sub [|foo|](of T)()
        [|foo|](of T)()
    End Sub

    Public Sub [|foo|](s As String)
        [|foo|](s)
    End Sub

    Public Shared Sub [|foo|](d As Double)
        [|foo|](d)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameOverloads:=True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(960955)>
        Public Async Function RenameParameterShouldNotAffectCommentsInOtherDocuments() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Public Class Program
    Sub Main([|$$args|] As String())

    End Sub
End Class
                            </Document>
                            <Document>
' args
                            </Document>
                        </Project>
                    </Workspace>)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "args", "bar", renameInComments:=True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1040098)>
        Public Async Function RenameInLinkedFilesDoesNotCrash() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[public class [|$$C|] { } // [|C|]]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB", renameInComments:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1040098)>
        Public Async Function RenameInLinkedFilesHandlesBothProjects() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[
public partial class [|$$C|] { } 
// [|C|]
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                            <Document FilePath="C2.cs"><![CDATA[
public partial class C { } 
// [|C|]
]]></Document>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB", renameInComments:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1040098)>
        Public Async Function RenameInLinkedFilesWithPrivateAccessibility() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[
public partial class C { private void [|$$F|](){} } 
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2" AssemblyName="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                            <Document FilePath="C2.cs"><![CDATA[
public partial class C { } 
// [|F|]
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj3">
                            <ProjectReference>Proj2</ProjectReference>
                            <Document FilePath="C3.cs"><![CDATA[
// F
]]></Document>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "F", "AB", renameInComments:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1040098)>
        Public Async Function RenameInLinkedFilesWithPublicAccessibility() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[
public partial class C { public void [|$$F|](){} } 
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2" AssemblyName="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                            <Document FilePath="C2.cs"><![CDATA[
public partial class C { } 
// [|F|]
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj3">
                            <ProjectReference>Proj2</ProjectReference>
                            <Document FilePath="C3.cs"><![CDATA[
// [|F|]
]]></Document>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "F", "AB", renameInComments:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(3623, "https://github.com/dotnet/roslyn/issues/3623")>
        Public Async Function RenameTypeInLinkedFiles() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                            <Document FilePath="C.cs"><![CDATA[
public class [|$$C|] { }
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925), WorkItem(1486, "https://github.com/dotnet/roslyn/issues/1486")>
        Public Async Function RenameInCommentsAndStringsCSharp() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
class Program
{
    /// <[|foo|]> [|foo|]! </[|foo|]>
    public void [|$$foo|]()
    {
        // [|foo|]  FOO
        /* [|foo|] */
        [|foo|]();

        var a = "foo";
        var b = $"{1}foo{2}";
    }

    public void foo(int i)
    {
        foo(i);
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameInComments:=True)

            workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
class Program
{
    /// <[|foo|]> [|foo|]! </[|foo|]>
    public void [|$$foo|]()
    {
        // [|foo|]  FOO
        /* [|foo|] */
        [|foo|]();

        var a = "foo";
        var b = $"{1}foo{2}";
    }

    public void [|foo|](int i)
    {
        [|foo|](i);
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameOverloads:=True, renameInComments:=True)

            workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
class Program
{
    /// <[|foo|]> [|foo|]! </[|foo|]>
    public void [|$$foo|]()
    {
        // [|foo|]  FOO
        /* [|foo|] */
        [|foo|]();

        var a = "[|foo|]";
        var b = $"{1}[|foo|]{2}";
    }

    public void foo(int i)
    {
        foo(i);
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameInComments:=True, renameInStrings:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925), WorkItem(1486, "https://github.com/dotnet/roslyn/issues/1486")>
        Public Async Function RenameInCommentsAndStringsVisualBasic() As Task
            Dim workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                <![CDATA[
Class Program
	''' <[|foo|]> [|foo|]! </[|foo|]>
	Public Sub [|$$foo|]()
		' [|foo|]  FOO
		' [|foo|]
		[|foo|]()

		Dim a = "foo"
		Dim b = $"{1}foo{2}"
	End Sub

	Public Sub foo(i As Integer)
		foo(i)
	End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameInComments:=True)

            workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            <![CDATA[
Class Program
	''' <[|foo|]> [|foo|]! </[|foo|]>
	Public Sub [|$$foo|]()
		' [|foo|]  FOO
		' [|foo|]
		[|foo|]()

		Dim a = "foo"
		Dim b = $"{1}foo{2}"
	End Sub

	Public Sub [|foo|](i As Integer)
		[|foo|](i)
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameOverloads:=True, renameInComments:=True)

            workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            <![CDATA[
Class Program
	''' <[|foo|]> [|foo|]! </[|foo|]>
	Public Sub [|$$foo|]()
		' [|foo|]  FOO
		' [|foo|]
		[|foo|]()

		Dim a = "[|foo|]"
		Dim b = $"{1}[|foo|]{2}"
	End Sub

	Public Sub foo(i As Integer)
		foo(i)
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>)

            Await VerifyRenameOptionChangedSessionCommit(workspace, "foo", "bar", renameInComments:=True, renameInStrings:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub SimpleEditAndCancel()
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

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                session.Cancel()

                ' Assert the file is what it started as
                Assert.Equal(workspace.Documents.Single().InitialTextSnapshot.GetText(), textBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub


        <WpfFact>
        <WorkItem(539513)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CanRenameTypeNamedDynamic() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$dynamic|]
                                {
                                    void M()
                                    {
                                        [|dynamic|] d;
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "foo")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace, "foodynamic")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ReadOnlyRegionsCreated()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class $$C
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)
                Dim buffer = workspace.Documents.Single().TextBuffer

                ' Typing at the beginning and end of our span should work
                Dim cursorPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Assert.False(buffer.IsReadOnly(cursorPosition))
                Assert.False(buffer.IsReadOnly(cursorPosition + 1))

                ' Replacing our span should work
                Assert.False(buffer.IsReadOnly(New Span(workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value, length:=1)))

                ' Make sure we can't type at the start or end
                Assert.True(buffer.IsReadOnly(0))
                Assert.True(buffer.IsReadOnly(buffer.CurrentSnapshot.Length))

                session.Cancel()
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(543018)>
        Public Sub ReadOnlyRegionsCreatedWhichHandleBeginningOfFileEdgeCase()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>$$C c; class C { }</Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)
                Dim buffer = workspace.Documents.Single().TextBuffer

                ' Typing at the beginning and end of our span should work
                Assert.False(buffer.IsReadOnly(0))
                Assert.False(buffer.IsReadOnly(1))

                ' Replacing our span should work
                Assert.False(buffer.IsReadOnly(New Span(workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value, length:=1)))

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameWithInheritenceCascadingWithClass()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                abstract class AAAA
                                {
                                    public abstract void [|Foo|]();
                                }

                                class BBBB : AAAA
                                {
                                    public override void [|Foo|]() { }
                                }

                                class DDDD : BBBB
                                {
                                    public override void [|Foo|]() { }
                                }
                                class CCCC : AAAA
                                {
                                    public override void [|$$Foo|]() { }
                                }
                            </Document>
                        </Project>
                    </Workspace>, renameTo:="FooBar")


            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(530467)>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameTyping() As Task
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

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameTyping2() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Foo|]
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(579210)>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameCommit() As Task
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

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(530765)>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameCancel() As Task
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

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                session.Cancel()
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyRenameTrackingWorksAfterInlineRenameCommit() As Task
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

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)

                textBuffer.Insert(caretPosition, "Baz")
                Await VerifyRenameTrackingTags(renameTrackingTagger, workspace, document, expectedTagCount:=1)
            End Using
        End Function

        <WpfFact, WorkItem(978099)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyPreviewChangesCalled() As Task
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

                ' Preview should not return null
                Dim previewService = DirectCast(workspace.Services.GetService(Of IPreviewDialogService)(), MockPreviewDialogService)
                previewService.ReturnsNull = False

                Dim session = StartSession(workspace)
                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit(previewChanges:=True)

                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Assert.True(previewService.Called)
                Assert.Equal(String.Format(EditorFeaturesResources.PreviewChangesOf, EditorFeaturesResources.Rename), previewService.Title)
                Assert.Equal(String.Format(EditorFeaturesResources.RenameToTitle, "Foo", "BarFoo"), previewService.Description)
                Assert.Equal("Foo", previewService.TopLevelName)
                Assert.Equal(Glyph.ClassInternal, previewService.TopLevelGlyph)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyPreviewChangesCancellation() As Task
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

                Dim previewService = DirectCast(workspace.Services.GetService(Of IPreviewDialogService)(), MockPreviewDialogService)
                previewService.ReturnsNull = True

                Dim session = StartSession(workspace)
                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit(previewChanges:=True)

                Await VerifyTagsAreCorrect(workspace, "BarFoo")
                Assert.True(previewService.Called)

                ' Session should still be up; type some more
                caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                textBuffer.Insert(caretPosition, "Cat")

                previewService.ReturnsNull = False
                previewService.Called = False
                session.Commit(previewChanges:=True)
                Await VerifyTagsAreCorrect(workspace, "CatBarFoo")
                Assert.True(previewService.Called)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_MethodWithReferences() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj" PreprocessorSymbols="Proj1=True">
                            <Document FilePath="C.vb">
Class C
    Sub [|M$$|]()
    End Sub

    Sub Test()
#If Proj1 Then
        [|M|]()
#End If
#If Proj2 Then
        [|M|]()
#End If
    End Sub
End Class
                              </Document>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="Proj2=True">
                            <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "o")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Mo")

                textBuffer.Insert(caretPosition + 1, "w")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Mow")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "Mow")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_FieldWithReferences() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj" PreprocessorSymbols="Proj1=True">
                            <Document FilePath="C.vb">
Class C
    Dim [|m$$|] As Integer

    Sub Test()
#If Proj1 Then
        Dim x = [|m|]
#End If
#If Proj2 Then
        Dim x = [|m|]
#End If
    End Sub
End Class
                              </Document>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="Proj2=True">
                            <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "ma")

                textBuffer.Insert(caretPosition + 1, "w")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "maw")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "maw")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        <WorkItem(554, "https://github.com/dotnet/roslyn/issues/554")>
        Public Async Function CodeActionCannotCommitDuringInlineRename() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                            <Document FilePath="C.cs">
class C
{
    void M()
    {
        var z = {|introducelocal:5 + 5|};
        var q = [|x$$|];
    }

    int [|x|];
}</Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer
                textBuffer.Insert(caretPosition, "yz")
                Await WaitForRename(workspace)

                ' Invoke a CodeAction
                Dim introduceVariableRefactoringProvider = New IntroduceVariableCodeRefactoringProvider()
                Dim actions = New List(Of CodeAction)
                Dim context = New CodeRefactoringContext(
                    workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id),
                    workspace.Documents.Single().AnnotatedSpans()("introducelocal").Single(),
                    Sub(a) actions.Add(a),
                    CancellationToken.None)

                workspace.Documents.Single().AnnotatedSpans.Clear()
                introduceVariableRefactoringProvider.ComputeRefactoringsAsync(context).Wait()

                Dim editHandler = workspace.ExportProvider.GetExportedValue(Of ICodeActionEditHandlerService)

                Dim actualSeverity As NotificationSeverity = Nothing
                Dim notificationService = DirectCast(workspace.Services.GetService(Of INotificationService)(), INotificationServiceCallback)
                notificationService.NotificationCallback = Sub(message, title, severity) actualSeverity = severity

                editHandler.Apply(
                    workspace,
                    workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id),
                    Await actions.First().GetOperationsAsync(CancellationToken.None),
                    "unused",
                    CancellationToken.None)

                ' CodeAction should be rejected
                Assert.Equal(NotificationSeverity.Error, actualSeverity)
                Assert.Equal("
class C
{
    void M()
    {
        var z = 5 + 5;
        var q = xyz;
    }

    int xyz;
}",
                    textBuffer.CurrentSnapshot.GetText())

                ' Rename should still be active
                Await VerifyTagsAreCorrect(workspace, "xyz")

                textBuffer.Insert(caretPosition + 2, "q")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "xyzq")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_NoOverloads() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|M$$|]()
    {
        nameof([|M|]).ToString();
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Ma")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "Ma")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromReference_NoOverloads() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|M|]()
    {
        nameof([|M$$|]).ToString();
    }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Ma")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "Ma")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_WithOverloads() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|M$$|]()
    {
        nameof(M).ToString();
    }

    void M(int x) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Ma")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "Ma")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromReference_WithOverloads() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|M|]()
    {
        nameof([|M$$|]).ToString();
    }

    void [|M|](int x) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().TextBuffer

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace, "Ma")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace, "Ma")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_WithOverloads_WithRenameOverloadsOption() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class C
{
    void [|$$M|]()
    {
        nameof([|M|]).ToString();
    }

    void [|M|](int x) { }
}
                            </Document>
                        </Project>
                    </Workspace>)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "M", "Sa", renameOverloads:=True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1142095)>
        Public Async Function RenameCommitsWhenDebuggingStarts() As Task
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

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                ' Make sure the RenameService's ActiveSession is still there
                Dim renameService = workspace.GetService(Of IInlineRenameService)()
                Assert.NotNull(renameService.ActiveSession)

                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                ' Simulate starting a debugging session
                Dim editAndContinueWorkspaceService = workspace.Services.GetService(Of IEditAndContinueWorkspaceService)
                editAndContinueWorkspaceService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run)

                ' Ensure the rename was committed
                Assert.Null(renameService.ActiveSession)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(1142095)>
        Public Async Function RenameCommitsWhenExitingDebuggingBreakMode() As Task
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

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "Bar")

                ' Make sure the RenameService's ActiveSession is still there
                Dim renameService = workspace.GetService(Of IInlineRenameService)()
                Assert.NotNull(renameService.ActiveSession)

                Await VerifyTagsAreCorrect(workspace, "BarFoo")

                ' Simulate ending break mode in the debugger (by stepping or continuing)
                Dim editAndContinueWorkspaceService = workspace.Services.GetService(Of IEditAndContinueWorkspaceService)
                editAndContinueWorkspaceService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run)

                ' Ensure the rename was committed
                Assert.Null(renameService.ActiveSession)
                Await VerifyTagsAreCorrect(workspace, "BarFoo")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(3316, "https://github.com/dotnet/roslyn/issues/3316")>
        Public Async Function InvalidInvocationExpression() As Task
            ' Everything on the last line of main is parsed as a single invocation expression
            ' with CType(...) as the receiver and everything else as arguments.
            ' Rename doesn't expect to see CType as the receiver of an invocation.
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Module Module1
    Sub Main()
        Dim [|$$p|] As IEnumerable(Of Integer) = {1, 2, 3}
        Dim linked = Enumerable.Aggregate(Of Global.&lt;anonymous type:head As Global.System.Int32, tail As Global.System.Object&gt;)(
            CType([|p|], IEnumerable(Of Integer)), Nothing, Function(total, curr) Nothing)
    End Sub
End Module
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Insert(caretPosition, "q")
                session.Commit()

                Await VerifyTagsAreCorrect(workspace, "qp")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(2445, "https://github.com/dotnet/roslyn/issues/2445")>
        Public Async Function InvalidExpansionTarget() As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                int x;
                                x = 2;
                                void [|$$M|]() { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().TextBuffer

                textBuffer.Delete(New Span(caretPosition, 1))
                textBuffer.Insert(caretPosition, "x")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace, "x")
            End Using
        End Function
    End Class
End Namespace
