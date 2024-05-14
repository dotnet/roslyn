' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class InheritsGraphQueryTests
        <WpfFact>
        Public Async Function BaseTypesOfSimpleType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C : System.IDisposable { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritsGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@2 Namespace=System Type=Object)" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="Object" Icon="Microsoft.VisualStudio.Class.Public" Label="Object"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@2 Namespace=System Type=Object)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/FxReferenceAssembliesUri"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546199")>
        Public Async Function TestErrorBaseType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C : A { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritsGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A)" Category="CodeSchema_Type" CommonLabel="A" Icon="Microsoft.VisualStudio.Error.Public" Label="A"/>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@1 Type=A)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestSolutionWithMultipleProjects() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" AssemblyName="ProjectA" CommonReferences="true">
                            <Document FilePath="Z:\ProjectA.cs">public class A { }</Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ProjectB" CommonReferences="true">
                            <ProjectReference>ProjectA</ProjectReference>
                            <Document FilePath="Z:\ProjectB.cs">public class B : A { }</Document>
                        </Project>
                        <Project Language="C#" AssemblyName="ProjectC" CommonReferences="true">
                            <ProjectReference>ProjectB</ProjectReference>
                            <Document FilePath="Z:\ProjectC.cs">public class C : B$$ { }</Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New InheritsGraphQuery(), GraphContextDirection.Target)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=A)" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="A" Icon="Microsoft.VisualStudio.Class.Public" Label="A"/>
                            <Node Id="(@2 Type=B)" Category="CodeSchema_Class" CodeSchemaProperty_IsPublic="True" CommonLabel="B" Icon="Microsoft.VisualStudio.Class.Public" Label="B"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@2 Type=B)" Target="(@1 Type=A)" Category="InheritsFrom"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/ProjectA.dll"/>
                            <Alias n="2" Uri="Assembly=file:///Z:/ProjectB.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
