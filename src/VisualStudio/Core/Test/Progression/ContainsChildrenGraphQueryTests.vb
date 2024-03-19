' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.GraphModel.Schemas
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class ContainsChildrenGraphQueryTests
        <WpfFact>
        Public Async Function ContainsChildrenForDocument() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                Dim node = outputContext.Graph.Nodes.Single()

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="True" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function ContainsChildrenForEmptyDocument() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="False" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/27805")>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/233666")>
        <WpfFact>
        Public Async Function ContainsChildrenForFileWithIllegalPath() As Task
            Using testState = ProgressionTestState.Create(<Workspace/>)
                Dim graph = New Graph
                graph.Nodes.GetOrCreate(
                    GraphNodeId.GetNested(GraphNodeId.GetPartial(CodeGraphNodeIdName.File, New Uri("C:\path\to\""some folder\App.config""", UriKind.RelativeOrAbsolute))),
                    label:=String.Empty,
                    CodeNodeCategories.File)

                ' Just making sure it doesn't throw.
                Dim outputContext = Await testState.GetGraphContextAfterQuery(graph, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789685")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/794846")>
        <WpfFact>
        Public Async Function ContainsChildrenForNotYetLoadedSolution() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")

                ' To simulate the situation where a solution is not yet loaded and project info is not available,
                ' remove a project from the solution.

                Dim oldSolution = testState.GetSolution()
                Dim newSolution = oldSolution.RemoveProject(oldSolution.ProjectIds.FirstOrDefault())
                Dim outputContext = Await testState.GetGraphContextAfterQueryWithSolution(inputGraph, newSolution, New ContainsChildrenGraphQuery(), GraphContextDirection.Self)

                ' ContainsChildren should be set to false, so following updates will be tractable.

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" ContainsChildren="False" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)

            End Using
        End Function

        <WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/165369")>
        <WpfFact>
        Public Async Function ContainsChildrenForNodeWithRelativeUriPath() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class C
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                ' Force creation of a graph node that has a nested relative URI file path.  This simulates nodes that
                ' other project types can give us for non-code files.  E.g., `favicon.ico` for web projects.
                Dim nodeId = GraphNodeId.GetNested(GraphNodeId.GetPartial(CodeGraphNodeIdName.File, New Uri("/Z:/Project.vb", UriKind.Relative)))
                Dim inputGraph = New Graph()
                Dim node = inputGraph.Nodes.GetOrCreate(nodeId)
                node.AddCategory(CodeNodeCategories.File)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsChildrenGraphQuery(), GraphContextDirection.Any)
                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1)" Category="File" ContainsChildren="False"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="File=/Z:/Project.vb"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
