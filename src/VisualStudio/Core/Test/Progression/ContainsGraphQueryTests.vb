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
    Public Class ContainsGraphQueryTests
        <WpfFact>
        Public Async Function TypesContainedInCSharpDocument() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                                enum E { }
                                interface I { }
                                struct S { }
                                record R1 { }
                                record R2;
                                record class R3;
                                record struct R4 { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@3 Type=E)" Category="CodeSchema_Enum" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Enum.Internal" Label="E"/>
                            <Node Id="(@3 Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I"/>
                            <Node Id="(@3 Type=R1)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="R1" Icon="Microsoft.VisualStudio.Class.Internal" Label="R1"/>
                            <Node Id="(@3 Type=R2)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="R2" Icon="Microsoft.VisualStudio.Class.Internal" Label="R2"/>
                            <Node Id="(@3 Type=R3)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="R3" Icon="Microsoft.VisualStudio.Class.Internal" Label="R3"/>
                            <Node Id="(@3 Type=R4)" Category="CodeSchema_Struct" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="R4" Icon="Microsoft.VisualStudio.Struct.Internal" Label="R4"/>
                            <Node Id="(@3 Type=S)" Category="CodeSchema_Struct" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="S" Icon="Microsoft.VisualStudio.Struct.Internal" Label="S"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=C)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=E)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=I)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=R1)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=R2)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=R3)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=R4)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=S)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TypesContainedInCSharpDocumentInsideNamespace() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                            namespace N.M
                            {
                                class C { }
                                enum E { }
                                interface I { }
                                struct S { }
                            }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Namespace=N.M Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@3 Namespace=N.M Type=E)" Category="CodeSchema_Enum" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Enum.Internal" Label="E"/>
                            <Node Id="(@3 Namespace=N.M Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I"/>
                            <Node Id="(@3 Namespace=N.M Type=S)" Category="CodeSchema_Struct" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="S" Icon="Microsoft.VisualStudio.Struct.Internal" Label="S"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=N.M Type=C)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=N.M Type=E)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=N.M Type=I)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=N.M Type=S)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function TypesContainedInVisualBasicDocument() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class C
                                End Class

                                Enum E
                                End Enum

                                Interface I
                                End Interface

                                Module M
                                End Module
    
                                Structure S
                                End Structure
                         </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.vb")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                            <Node Id="(@3 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                            <Node Id="(@3 Type=E)" Category="CodeSchema_Enum" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Enum.Internal" Label="E"/>
                            <Node Id="(@3 Type=I)" Category="CodeSchema_Interface" CodeSchemaProperty_IsAbstract="True" CodeSchemaProperty_IsInternal="True" CommonLabel="I" Icon="Microsoft.VisualStudio.Interface.Internal" Label="I"/>
                            <Node Id="(@3 Type=M)" Category="CodeSchema_Module" CodeSchemaProperty_IsInternal="True" CommonLabel="M" Icon="Microsoft.VisualStudio.Module.Internal" Label="M"/>
                            <Node Id="(@3 Type=S)" Category="CodeSchema_Struct" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="S" Icon="Microsoft.VisualStudio.Struct.Internal" Label="S"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=C)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=E)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=I)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=M)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=S)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.vb"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function MembersContainedInCSharpScriptDocument() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.csx" Kind="Script">
                                <ParseOptions Kind="Script"/>
                                int F;
                                int P { get; set; }
                                void M()
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.csx")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains).ConfigureAwait(False)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.csx"/>
                            <Node Id="(@3 Type=Script Member=F)" Category="CodeSchema_Field" CodeSchemaProperty_IsPrivate="True" CommonLabel="F" Icon="Microsoft.VisualStudio.Field.Private" Label="F"/>
                            <Node Id="(@3 Type=Script Member=M)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="M" Icon="Microsoft.VisualStudio.Method.Private" Label="M"/>
                            <Node Id="(@3 Type=Script Member=P)" Category="CodeSchema_Property" CodeSchemaProperty_IsPrivate="True" CommonLabel="P" Icon="Microsoft.VisualStudio.Property.Private" Label="P"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=Script Member=F)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=Script Member=M)" Category="Contains"/>
                            <Link Source="(@1 @2)" Target="(@3 Type=Script Member=P)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.csx"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact>
        Public Async Function MembersContainedInClass() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C { void M(); event System.EventHandler E; }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=C Member=E)" Category="CodeSchema_Event" CodeSchemaProperty_IsPrivate="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Event.Private" Label="E"/>
                            <Node Id="(@1 Type=C Member=M)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="M" Icon="Microsoft.VisualStudio.Method.Private" Label="M"/>
                            <Node Id="(@1 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=C)" Target="(@1 Type=C Member=E)" Category="Contains"/>
                            <Link Source="(@1 Type=C)" Target="(@1 Type=C Member=M)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543892")>
        Public Async Function NestedTypesContainedInClass() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { class $$D { class E { } } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=(Name=D ParentType=C))" Category="CodeSchema_Class" CodeSchemaProperty_IsPrivate="True" CommonLabel="D" Icon="Microsoft.VisualStudio.Class.Private" Label="D"/>
                            <Node Id="(@1 Type=(Name=E ParentType=(Name=D ParentType=C)))" Category="CodeSchema_Class" CodeSchemaProperty_IsPrivate="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Class.Private" Label="E"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=(Name=D ParentType=C))" Target="(@1 Type=(Name=E ParentType=(Name=D ParentType=C)))" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545018")>
        Public Async Function EnumMembersInEnum() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                enum $$E { M }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = Await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=E Member=M)" Category="CodeSchema_Field" CodeSchemaProperty_IsPublic="True" CodeSchemaProperty_IsStatic="True" CommonLabel="M" Icon="Microsoft.VisualStudio.EnumMember.Public" Label="M"/>
                            <Node Id="(@1 Type=E)" Category="CodeSchema_Enum" CodeSchemaProperty_IsFinal="True" CodeSchemaProperty_IsInternal="True" CommonLabel="E" Icon="Microsoft.VisualStudio.Enum.Internal" Label="E"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 Type=E)" Target="(@1 Type=E Member=M)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610147")>
        Public Async Function NothingInBrokenCode() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                [(delegate static
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610147")>
        Public Async Function NothingInBrokenCode2() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                int this[] { get { int x; } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = testState.GetGraphWithDocumentNode(filePath:="Z:\Project.cs")
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608653")>
        Public Async Function NothingInBrokenCode3() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Module $$Module1
                                    Public Class
                                End Module
                            </Document>
                        </Project>
                    </Workspace>)

                Dim inputGraph = await testState.GetGraphWithMarkedSymbolNodeAsync()
                Dim outputContext = Await testState.GetGraphContextAfterQuery(inputGraph, New ContainsGraphQuery(), GraphContextDirection.Contains)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 Type=Module1)" Category="CodeSchema_Module" CodeSchemaProperty_IsInternal="True" CommonLabel="Module1" Icon="Microsoft.VisualStudio.Module.Internal" Label="Module1"/>
                        </Nodes>
                        <Links/>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function
    End Class
End Namespace
