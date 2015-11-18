' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class InheritsGraphQueryTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function BaseTypesOfSimpleType() As Threading.Tasks.Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C : System.IDisposable { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
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

        <WorkItem(546199)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestErrorBaseType() As Threading.Tasks.Task
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C : A { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestSolutionWithMultipleProjects() As Threading.Tasks.Task
            Using testState = New ProgressionTestState(
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

                Dim inputGraph = testState.GetGraphWithMarkedSymbolNode()
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
