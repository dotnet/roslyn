' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.NavigateTo
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class SearchGraphQueryTests_NavigateTo
        <WpfFact>
        Public Async Function SearchForType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("C", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 C)" Category="CodeSchema_Class" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 C)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem(545474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545474")>
        Public Async Function SearchForNestedType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { class F { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("F", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 F)" Category="CodeSchema_Class" Icon="Microsoft.VisualStudio.Class.Private" Label="F"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 F)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForMember() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void M(); }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("M", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 M())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Private" Label="M()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 M())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForPartialType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Namespace N
    Partial Class C
        Sub Goo()
        End Sub
    End Class
End Namespace
                            </Document>
                            <Document FilePath="Z:\Project2.vb">
Namespace N
    Partial Class C
        Sub Bar()
        End Sub
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("C", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 C)" Category="CodeSchema_Class" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project2.vb"/>
                            <Node Id="(@1 @3 C)" Category="CodeSchema_Class" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@1 @3)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 C)" Category="Contains"/>
                            <Link Source="(@1 @3)" Target="(@1 @3 C)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project2.vb"/>
                            <Alias n="3" Uri="File=file:///Z:/Project.vb"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForMethodInPartialType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Namespace N
    Partial Class C
        Sub Goo()
        End Sub
    End Class
End Namespace
                            </Document>
                            <Document FilePath="Z:\Project2.vb">
Namespace N
    Partial Class C
        Sub Bar()
        End Sub
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("Goo", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 Goo())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Public" Label="Goo()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 Goo())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.vb"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchWithResultsAcrossMultipleTypeParts() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
Namespace N
    Partial Class C
        Sub ZGoo()
        End Sub
    End Class
End Namespace
                            </Document>
                            <Document FilePath="Z:\Project2.vb">
Namespace N
    Partial Class C
        Sub ZBar()
        End Sub
    End Class
End Namespace
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("Z", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 ZBar())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Public" Label="ZBar()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project2.vb"/>
                            <Node Id="(@1 @3 ZGoo())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Public" Label="ZGoo()"/>
                            <Node Id="(@1 @3)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 ZBar())" Category="Contains"/>
                            <Link Source="(@1 @3)" Target="(@1 @3 ZGoo())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project2.vb"/>
                            <Alias n="3" Uri="File=file:///Z:/Project.vb"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForDottedName1() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class Dog { void Bark() { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("D.B", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 Bark())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 Bark())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForDottedName2() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class Dog { void Bark() { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("C.B", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes/>
                        <Links/>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForDottedName3() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("D.B", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 Bark())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 Bark())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchForDottedName4() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("A.D.B", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2 Bark())" Category="CodeSchema_Method" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark()"/>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@1 @2 Bark())" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function SearchWithNullFilePathsOnProject() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath=<%= TestWorkspace.NullFilePath %>>
                            <Document FilePath="Z:\SomeVenusDocument.aspx.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim threadingContext = testState.Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim outputContext = Await testState.GetGraphContextAfterQuery(
                    New Graph(), New SearchGraphQuery("A.D.B", NavigateToSearchScope.AllDocuments, threadingContext, AsynchronousOperationListenerProvider.NullListener), GraphContextDirection.Custom)

                ' When searching, don't descend into projects with a null FilePath because they are artifacts and not
                ' representable in the Solution Explorer, e.g., Venus projects create sub-projects with a null file
                ' path for each .aspx file.  Documents, on the other hand, are never allowed to have a null file path
                ' and as such are not tested here.  The project/document structure for these scenarios would look
                ' similar to this:
                '
                '    Project: SomeVenusProject, FilePath=C:\path\to\project.csproj
                '      + Document: SomeVenusDocument.aspx, FilePath=C:\path\to\SomeVenusDocument.aspx
                '        + Project: 1_SomeNamespace_SomeVenusDocument.aspx, FilePath=null        <- the problem is here
                '          + Document: SomeVenusDocument.aspx.cs
                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes/>
                        <Links/>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
