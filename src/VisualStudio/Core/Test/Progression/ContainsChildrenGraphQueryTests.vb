﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.GraphModel.Schemas
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class ContainsChildrenGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

        <WorkItem(789685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789685")>
        <WorkItem(794846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/794846")>
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

        <WorkItem(165369, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/165369")>
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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
