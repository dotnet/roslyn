' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.InlineRename
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    Public Class InlineRenameTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function SimpleEditAndCommit(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
                VerifyFileName(workspace, "BarTest1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameLocalVariableInTopLevelStatement(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                object [|$$test|] = new object();
                                var other = [|test|];
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "renamed")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameLambdaDiscard(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class C
{
    void M()
    {
        C _ = null;
        System.Func<int, string, int> f = (int _, string [|$$_|]) => { _ = null; return 1; };
    }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, originalTextToRename:="_", renameTextPrefix:="change", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/46208")>
        Public Async Function RenameWithInvalidIdentifier(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
class [|Test1$$|]
{
}
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, True)

                Dim session = StartSession(workspace)

                Dim selectedSpan = workspace.DocumentWithCursor.CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()
                ' User could use copy & paste to enter invalid character
                textBuffer.Insert(selectedSpan, "<>")

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22495")>
        Public Async Function RenameDeconstructionForeachCollection(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System.Collections.Generic;
class Deconstructable
{
    void M(IEnumerable<Deconstructable> [|$$x|])
    {
        foreach (var (y1, y2) in [|x|])
        {
        }
    }
    void Deconstruct(out int i, out int j) { i = 0; j = 0; }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "x", "change", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameDeconstructMethodInDeconstructionForeach(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document><![CDATA[
using System.Collections.Generic;
class Deconstructable
{
    void M(IEnumerable<Deconstructable> x)
    {
        foreach (var (y1, y2) in x)
        {
        }
        var (z1, z2) = this;
        [|Deconstruct|](out var t1, out var t2);
    }
    void [|$$Deconstruct|](out int i, out int j) { i = 0; j = 0; }
}
                            ]]></Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "Deconstruct", "Changed", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540120")>
        Public Async Function SimpleEditAndVerifyTagsPropagatedAndCommit(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                Await WaitForRename(workspace)

                Await VerifyTagsAreCorrect(workspace)

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
                VerifyFileName(workspace, "BarTest1")
            End Using
        End Function

        Private Shared Async Function VerifyRenameOptionChangedSessionCommit(workspace As EditorTestWorkspace,
                                                           originalTextToRename As String,
                                                           renameTextPrefix As String,
                                                           Optional renameOverloads As Boolean = False,
                                                           Optional renameInStrings As Boolean = False,
                                                           Optional renameInComments As Boolean = False,
                                                           Optional renameFile As Boolean = False,
                                                           Optional fileToRename As DocumentId = Nothing) As Task

            Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
            globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, renameOverloads)
            globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, renameInStrings)
            globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, renameInComments)
            globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, renameFile)

            Dim session = StartSession(workspace)

            ' Type a bit in the file
            Dim renameDocument = workspace.DocumentWithCursor
            renameDocument.GetTextBuffer().Insert(renameDocument.CursorPosition.Value, renameTextPrefix)

            Dim replacementText = renameTextPrefix + originalTextToRename
            Await WaitForRename(workspace)

            Await VerifyTagsAreCorrect(workspace)

            session.Commit()

            Await VerifyTagsAreCorrect(workspace)

            If renameFile Then
                If fileToRename Is Nothing Then
                    VerifyFileName(workspace, replacementText)
                Else
                    VerifyFileName(workspace.CurrentSolution.GetDocument(fileToRename), replacementText)
                End If
            End If
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/13186")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700921")>
        Public Async Function RenameOverloadsCSharp(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class Program
{
    public void [|$$goo|]()
    {
        [|goo|]();
    }

    public void [|goo|]&lt;T&gt;()
    {
        [|goo|]&lt;T&gt;();
    }

    public void [|goo|](int i)
    {
        [|goo|](i);
    }
}
                            </Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700921")>
        Public Async Function RenameOverloadsVisualBasic(host As RenameTestHost) As Task
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

    Public Sub [|$$goo|]()
        [|goo|]()
    End Sub

    Public Sub [|goo|](of T)()
        [|goo|](of T)()
    End Sub

    Public Sub [|goo|](s As String)
        [|goo|](s)
    End Sub

    Public Shared Sub [|goo|](d As Double)
        [|goo|](d)
    End Sub
End Class
                            </Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960955")>
        Public Async Function RenameParameterShouldNotAffectCommentsInOtherDocuments(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "args", "bar", renameInComments:=True)

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "Test1.vb"))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040098")>
        Public Async Function RenameInLinkedFilesDoesNotCrash(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[public class [|$$C|] { } // [|C|]]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB", renameInComments:=True)

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "C.cs"))

                ' https://github.com/dotnet/roslyn/issues/36075
                ' VerifyFileRename(workspace, "ABC")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040098")>
        Public Async Function RenameInLinkedFilesHandlesBothProjects(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
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
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB", renameInComments:=True)

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "C.cs"))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040098")>
        Public Async Function RenameInLinkedFilesWithPrivateAccessibility(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
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
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "F", "AB", renameInComments:=True)

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "C.cs"))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040098")>
        Public Async Function RenameInLinkedFilesWithPublicAccessibility(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
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
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "F", "AB", renameInComments:=True)

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "C.cs"))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/3623")>
        Public Async Function RenameTypeInLinkedFiles(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                            <Document FilePath="C.cs"><![CDATA[
public class [|$$C|] { }
]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "AB")

                Assert.NotNull(workspace.Documents.FirstOrDefault(Function(document) document.Name = "C.cs"))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925"), WorkItem("https://github.com/dotnet/roslyn/issues/1486")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        Public Async Function RenameInCommentsAndStringsCSharp(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.[|goo|]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.goo(System.Int32)")]

class Program
{
    /// <[|goo|]> [|goo|]! </[|goo|]>
    public void [|$$goo|]()
    {
        // [|goo|]  GOO
        /* [|goo|] */
        [|goo|]();

        var a = "goo";
        var b = $"{1}goo{2}";
    }

    public void goo(int i)
    {
        goo(i);
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameInComments:=True)
                VerifyFileName(workspace, "Test1")
            End Using

            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.[|goo|]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.[|goo|](System.Int32)")]

class Program
{
    /// <[|goo|]> [|goo|]! </[|goo|]>
    public void [|$$goo|]()
    {
        // [|goo|]  GOO
        /* [|goo|] */
        [|goo|]();

        var a = "goo";
        var b = $"{1}goo{2}";
    }

    public void [|goo|](int i)
    {
        [|goo|](i);
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameOverloads:=True, renameInComments:=True)
                VerifyFileName(workspace, "Test1")
            End Using

            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.[|goo|]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:Program.[|goo|](System.Int32)")]

class Program
{
    /// <[|goo|]> [|goo|]! </[|goo|]>
    public void [|$$goo|]()
    {
        // [|goo|]  GOO
        /* [|goo|] */
        [|goo|]();

        var a = "[|goo|]";
        var b = $"{1}[|goo|]{2}";
    }

    public void goo(int i)
    {
        goo(i);
    }
}]]>
                        </Document>
                    </Project>
                </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameInComments:=True, renameInStrings:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925"), WorkItem("https://github.com/dotnet/roslyn/issues/1486")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        Public Async Function RenameInCommentsAndStringsVisualBasic(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                                <![CDATA[
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.[|goo|]")>
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.goo(System.Int32)")>

Class Program
	''' <[|goo|]> [|goo|]! </[|goo|]>
	Public Sub [|$$goo|]()
		' [|goo|]  GOO
		' [|goo|]
		[|goo|]()

		Dim a = "goo"
		Dim b = $"{1}goo{2}"
	End Sub

	Public Sub goo(i As Integer)
		goo(i)
	End Sub
End Class
]]>
                            </Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameInComments:=True)
                VerifyFileName(workspace, "Test1")
            End Using

            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            <![CDATA[
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.[|goo|]")>
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.[|goo|](System.Int32)")>

Class Program
	''' <[|goo|]> [|goo|]! </[|goo|]>
	Public Sub [|$$goo|]()
		' [|goo|]  GOO
		' [|goo|]
		[|goo|]()

		Dim a = "goo"
		Dim b = $"{1}goo{2}"
	End Sub

	Public Sub [|goo|](i As Integer)
		[|goo|](i)
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameOverloads:=True, renameInComments:=True)
                VerifyFileName(workspace, "Test1")
            End Using

            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            <![CDATA[
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.[|goo|]")>
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope:="member", Target:="~M:Program.[|goo|](System.Int32)")>

Class Program
	''' <[|goo|]> [|goo|]! </[|goo|]>
	Public Sub [|$$goo|]()
		' [|goo|]  GOO
		' [|goo|]
		[|goo|]()

		Dim a = "[|goo|]"
		Dim b = $"{1}[|goo|]{2}"
	End Sub

	Public Sub goo(i As Integer)
		goo(i)
	End Sub
End Class
]]>
                        </Document>
                    </Project>
                </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "goo", "bar", renameInComments:=True, renameInStrings:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub SimpleEditAndCancel(host As RenameTestHost)
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()
                Dim initialTextSnapshot = textBuffer.CurrentSnapshot

                textBuffer.Insert(caretPosition, "Bar")

                session.Cancel()

                ' Assert the file is what it started as
                Assert.Equal(initialTextSnapshot.GetText(), textBuffer.CurrentSnapshot.GetText())

                ' Assert the file name didn't change
                VerifyFileName(workspace, "Test1.cs")
            End Using
        End Sub

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539513")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function CanRenameTypeNamedDynamic(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "goo")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ReadOnlyRegionsCreated(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class $$C
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)
                Dim buffer = workspace.Documents.Single().GetTextBuffer()

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

                ' Assert the file name didn't change
                VerifyFileName(workspace, "Test1.cs")
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543018")>
        Public Sub ReadOnlyRegionsCreatedWhichHandleBeginningOfFileEdgeCase(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>$$C c; class C { }</Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)
                Dim buffer = workspace.Documents.Single().GetTextBuffer()

                ' Typing at the beginning and end of our span should work
                Assert.False(buffer.IsReadOnly(0))
                Assert.False(buffer.IsReadOnly(1))

                ' Replacing our span should work
                Assert.False(buffer.IsReadOnly(New Span(workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value, length:=1)))

                session.Cancel()

                VerifyFileName(workspace, "Test1.cs")
            End Using
        End Sub

        <Theory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameWithInheritenceCascadingWithClass(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                abstract class AAAA
                                {
                                    public abstract void [|Goo|]();
                                }

                                class BBBB : AAAA
                                {
                                    public override void [|Goo|]() { }
                                }

                                class DDDD : BBBB
                                {
                                    public override void [|Goo|]() { }
                                }
                                class CCCC : AAAA
                                {
                                    public override void [|$$Goo|]() { }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="GooBar")

            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530467")>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameTyping(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
                VerifyFileName(workspace, "BarTest1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameTyping2(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)

                Await VerifyTagsAreCorrect(workspace)
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
                VerifyFileName(workspace, "BarTest1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579210")>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameCommit(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
                VerifyFileName(workspace, "BarTest1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530765")>
        Public Async Function VerifyNoRenameTrackingAfterInlineRenameCancel(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Cancel()
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)

                VerifyFileName(workspace, "Test1.cs")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyRenameTrackingWorksAfterInlineRenameCommit(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                Dim document = workspace.Documents.Single()
                Dim renameTrackingTagger = CreateRenameTrackingTagger(workspace, document)

                textBuffer.Insert(caretPosition, "Bar")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
                Await VerifyNoRenameTrackingTags(renameTrackingTagger, workspace, document)
                VerifyFileName(workspace, "BarTest1")

                textBuffer.Insert(caretPosition, "Baz")
                Await VerifyRenameTrackingTags(renameTrackingTagger, workspace, document, expectedTagCount:=1)
            End Using
        End Function

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/978099")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyPreviewChangesCalled(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                ' Preview should not return null
                Dim previewService = DirectCast(workspace.Services.GetRequiredService(Of IPreviewDialogService)(), MockPreviewDialogService)
                previewService.ReturnsNull = False

                Dim session = StartSession(workspace)
                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit(previewChanges:=True)

                Await VerifyTagsAreCorrect(workspace)
                Assert.True(previewService.Called)
                Assert.Equal(String.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Rename), previewService.Title)
                Assert.Equal(String.Format(EditorFeaturesResources.Rename_0_to_1_colon, "Goo", "BarGoo"), previewService.Description)
                Assert.Equal("Goo", previewService.TopLevelName)
                Assert.Equal(Glyph.ClassInternal, previewService.TopLevelGlyph)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyPreviewChangesCancellation(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim previewService = DirectCast(workspace.Services.GetService(Of IPreviewDialogService)(), MockPreviewDialogService)
                previewService.ReturnsNull = True

                Dim session = StartSession(workspace)
                session.ApplyReplacementText("BarGoo", propagateEditImmediately:=True)
                session.Commit(previewChanges:=True)

                Await VerifyTagsAreCorrect(workspace)
                Assert.True(previewService.Called)

                ' Session should still be up
                session.ApplyReplacementText("CatBarGoo", propagateEditImmediately:=True)

                previewService.ReturnsNull = False
                previewService.Called = False
                session.Commit(previewChanges:=True)
                Await VerifyTagsAreCorrect(workspace)
                Assert.True(previewService.Called)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_MethodWithReferences(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "o")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                textBuffer.Insert(caretPosition + 1, "w")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function VerifyLinkedFiles_FieldWithReferences(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                textBuffer.Insert(caretPosition + 1, "w")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/554")>
        Public Async Function CodeActionCannotCommitDuringInlineRename(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()
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

                Await editHandler.ApplyAsync(
                    workspace,
                    workspace.CurrentSolution,
                    workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id),
                    Await actions.First().NestedActions.First().GetOperationsAsync(CancellationToken.None),
                    "unused",
                    CodeAnalysisProgress.None,
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
                Await VerifyTagsAreCorrect(workspace)

                textBuffer.Insert(caretPosition + 2, "q")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_NoOverloads(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromReference_NoOverloads(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_WithOverloads(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromReference_WithOverloads(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.First().GetTextBuffer()

                textBuffer.Insert(caretPosition, "a")
                Await WaitForRename(workspace)
                Await VerifyTagsAreCorrect(workspace)

                session.Commit()
                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameMethodWithNameof_FromDefinition_WithOverloads_WithRenameOverloadsOption(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "M", "Sa", renameOverloads:=True)
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/3316")>
        Public Async Function InvalidInvocationExpression(host As RenameTestHost) As Task
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
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "q")
                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2445")>
        Public Async Function InvalidExpansionTarget(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                int x;
                                x = 2;
                                void [|$$M|]() { }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Delete(New Span(caretPosition, 1))
                textBuffer.Insert(caretPosition, "x")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/9117")>
        Public Async Function VerifyVBRenameCrashDoesNotRepro(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
Public Class Class1 
  Public Property [|$$Field1|] As Integer
End Class 

Public Class Class2 
  Public Shared Property DataSource As IEnumerable(Of Class1) 
  Public ReadOnly Property Dict As IReadOnlyDictionary(Of Integer, IEnumerable(Of Class1)) = 
  ( 
    From data 
    In DataSource 
    Group By 
    data.Field1
    Into Group1 = Group 
  ).ToDictionary( 
    Function(group) group.Field1,
    Function(group) group.Group1) 
End Class 
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Delete(New Span(caretPosition, 1))
                textBuffer.Insert(caretPosition, "x")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14554")>
        Public Sub VerifyVBRenameDoesNotCrashOnAsNewClause(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                                <Workspace>
                                    <Project Language="Visual Basic" CommonReferences="true">
                                        <Document>
Class C
    Sub New(a As Action)
    End Sub

    Public ReadOnly Property Vm As C

    Public ReadOnly Property Crash As New C(Sub()
                                                Vm.Sav()
                                            End Sub)

    Public Function Sav$$() As Boolean
        Return False
    End Function

    Public Function Save() As Boolean
        Return False
    End Function
End Class
                                        </Document>
                                    </Project>
                                </Workspace>, host)

                Dim session = StartSession(workspace)

                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                ' Ensure the rename doesn't crash
                textBuffer.Insert(caretPosition, "e")
                session.Commit()
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyNoFileRenameAllowedForPartialType(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                partial class [|$$Goo|]
                                {
                                    void Blah()
                                    {
                                    }
                                }
                            </Document>
                            <Document>
                                partial class Goo
                                {
                                    void BlahBlah()
                                    {
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.TypeWithMultipleLocations, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyFileRenameAllowedForPartialTypeWithSingleLocation(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                partial class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.Allowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyFileRenameAllowedWithMultipleTypesOnMatchingName(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                    }
                                }

                                class Test2
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.Allowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyNoFileRenameAllowedWithMultipleTypes(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Goo|]
                                {
                                    void Blah()
                                    {
                                    }
                                }

                                class Test1
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.TypeDoesNotMatchFileName, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyEnumKindSupportsFileRename(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                enum [|$$Test1|]
                                {
                                    One,
                                    Two,
                                    Three
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.Allowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyInterfaceKindSupportsFileRename(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                interface [|$$Test1|]
                                {
                                    void Blah();
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.Allowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyUnsupportedFileRename(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                interface Test1
                                {
                                    void [|$$Blah|]();
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.NotAllowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyFileRenameNotAllowedForLinkedFiles(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                            <Document FilePath="C.cs"><![CDATA[public class [|$$C|] { } // [|C|]]]></Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                            <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                        </Project>
                    </Workspace>, host)

                ' Disable document changes to make sure file rename is not supported. 
                ' Linked workspace files will report that applying changes to document
                ' info is not allowed; this is intended to mimic that behavior
                ' and make sure inline rename works as intended.
                workspace.CanApplyChangeDocument = False

                Dim session = StartSession(workspace)

                Assert.Equal(InlineRenameFileRenameInfo.NotAllowed, session.FileRenameInfo)
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub VerifyFileRenamesCorrectlyWhenCaseChanges(host As RenameTestHost)
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
class [|$$Test1|]
{
}
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Delete(New Span(caretPosition, 1))
                textBuffer.Insert(caretPosition, "t")

                session.Commit()
                VerifyFileName(workspace, "test1")
            End Using
        End Sub

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/36063")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function EditBackToOriginalNameThenCommit(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|$$Test1|]
                                {
                                    void Blah()
                                    {
                                        [|Test1|] f = new [|Test1|]();
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")
                textBuffer.Delete(New Span(caretPosition, "Bar".Length))

                Dim committed = session.GetTestAccessor().CommitWorker(previewChanges:=False)
                Assert.False(committed)

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/44576")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameFromOtherFile(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class Test1
                            {
                                void Blah()
                                {
                                    [|$$Test2|] t2 = new [|Test2|]();
                                }
                            }
                        </Document>
                        <Document Name="Test2.cs">
                            class Test2
                            {
                            }
                        </Document>
                    </Project>
                </Workspace>, host)

                Dim docToRename = workspace.Documents.First(Function(doc) doc.Name = "Test2.cs")
                Await VerifyRenameOptionChangedSessionCommit(workspace, originalTextToRename:="Test2", renameTextPrefix:="Test2Changed", renameFile:=True, fileToRename:=docToRename.Id)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        Public Async Function RenameConstructorReferencedInGlobalSuppression(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                <![CDATA[
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:[|C|].#ctor")]

class [|C|]
{
    public [|$$C|]()
    {
    }
}]]>
                            </Document>
                        </Project>
                    </Workspace>, host)

                Await VerifyRenameOptionChangedSessionCommit(workspace, "C", "D")
                VerifyFileName(workspace, "Test1")
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameRecordWithNoBody1(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                record [|$$Goo|](int a);
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameClassWithNoBody1(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                class [|$$Goo|](int a);
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameStructWithNoBody1(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                struct [|$$Goo|](int a);
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameInterfaceWithNoBody1(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                interface [|$$Goo|](int a);
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameRecordWithBody(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                record [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameClassWithBody(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                class [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameStructWithBody(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                struct [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameInterfaceWithBody(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                interface [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g;
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameRecordConstructorCalled(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                record [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g = new [|Goo|](1);
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameClassConstructorCalled(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                class [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g = new [|Goo|](1);
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameStructConstructorCalled(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="9.0">
                            <Document>
                                struct [|$$Goo|](int a) { }
                                class C
                                {
                                    [|Goo|] g = new [|Goo|](1);
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameExtendedProperty(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
                            <Document>
                                class C
                                {
                                    void M()
                                    {
                                        _ = this is { Other.[|Goo|]: not null, Other.Other.[|Goo|]: not null, [|Goo|]: null } ;
                                    }

                                    public C [|$$Goo|] { get; set; }
                                    public C Other { get; set; }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim caretPosition = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
                Dim textBuffer = workspace.Documents.Single().GetTextBuffer()

                textBuffer.Insert(caretPosition, "Bar")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameInComment(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
                            <Document>
                                class [|$$MyClass|]
                                {
                                    /// <summary>
                                    /// Initializes <see cref="MyClass"/>;
                                    /// </summary>
                                    MyClass() 
                                    {
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                session.ApplyReplacementText("Example", True)
                session.RefreshRenameSessionWithOptionsChanged(New SymbolRenameOptions(RenameInComments:=True))

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66208")>
        Public Async Function RenameWithGeneratedFile(host As RenameTestHost) As Task
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" LanguageVersion="preview">
                            <Document>
                                partial class [|$$MyClass|]
                                {
                                    public void M1()
                                    {
                                    }
                                }
                            </Document>
                            <DocumentFromSourceGenerator>
                                partial class [|MyClass|]
                                {
                                    public void M2()
                                    {
                                    }
                                }
                            </DocumentFromSourceGenerator>
                        </Project>
                    </Workspace>, host)

                Dim session = StartSession(workspace)

                ' Type a bit in the file
                Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim caretPosition = cursorDocument.CursorPosition.Value
                Dim textBuffer = cursorDocument.GetTextBuffer()

                textBuffer.Insert(caretPosition, "Example")

                session.Commit()

                Await VerifyTagsAreCorrect(workspace)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameRecordParameter(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                public record MyRecord(string [|$$MyProperty|]);
                                public class ReferenceClass
                                {
                                    public static void Test()
                                    {
                                        var record = new MyRecord("HelloWorld");
                                        var c = record.[|MyProperty|];
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="MyNewProperty")
            End Using
        End Sub

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameRecordProperty(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                public record MyRecord(string [|MyProperty|]);
                                public class ReferenceClass
                                {
                                    public static void Test()
                                    {
                                        var record = new MyRecord("HelloWorld");
                                        var c = record.[|$$MyProperty|];
                                    }
                                }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="MyNewProperty")
            End Using
        End Sub
    End Class
End Namespace
