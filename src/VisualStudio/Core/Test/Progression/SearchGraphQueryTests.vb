' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <[UseExportProvider]>
    Public Class SearchGraphQueryTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="C"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=C)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545474")>
        Public Async Function SearchForNestedType() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { class F { } }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="F"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Type=(Name=F ParentType=C))" Category="CodeSchema_Class" CodeSchemaProperty_IsPrivate="True" CommonLabel="F" Icon="Microsoft.VisualStudio.Class.Private" Label="F"/>
                            <Node Id="(@3 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=C)" Category="Contains"/>
                            <Link Source="(@3 Type=C)" Target="(@3 Type=(Name=F ParentType=C))" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForMember() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void M(); }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="M"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Type=C Member=M)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="M" Icon="Microsoft.VisualStudio.Method.Private" Label="M"/>
                            <Node Id="(@3 Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=C)" Category="Contains"/>
                            <Link Source="(@3 Type=C)" Target="(@3 Type=C Member=M)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="C"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project2.vb"/>
                            <Node Id="(@1 @3)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                            <Node Id="(@4 Namespace=N Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@4 Namespace=N Type=C)" Category="Contains"/>
                            <Link Source="(@1 @3)" Target="(@4 Namespace=N Type=C)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project2.vb"/>
                            <Alias n="3" Uri="File=file:///Z:/Project.vb"/>
                            <Alias n="4" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="Goo"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                            <Node Id="(@3 Namespace=N Type=C Member=Goo)" Category="CodeSchema_Method" CodeSchemaProperty_IsHideBySignature="True" CodeSchemaProperty_IsPublic="True" CommonLabel="Goo" Icon="Microsoft.VisualStudio.Method.Public" Label="Goo"/>
                            <Node Id="(@3 Namespace=N Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=N Type=C)" Category="Contains"/>
                            <Link Source="(@3 Namespace=N Type=C)" Target="(@3 Namespace=N Type=C Member=Goo)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.vb"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
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

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="Z"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project2.vb"/>
                            <Node Id="(@1 @3)" Category="CodeSchema_ProjectItem" Label="Project.vb"/>
                            <Node Id="(@4 Namespace=N Type=C Member=ZBar)" Category="CodeSchema_Method" CodeSchemaProperty_IsHideBySignature="True" CodeSchemaProperty_IsPublic="True" CommonLabel="ZBar" Icon="Microsoft.VisualStudio.Method.Public" Label="ZBar"/>
                            <Node Id="(@4 Namespace=N Type=C Member=ZGoo)" Category="CodeSchema_Method" CodeSchemaProperty_IsHideBySignature="True" CodeSchemaProperty_IsPublic="True" CommonLabel="ZGoo" Icon="Microsoft.VisualStudio.Method.Public" Label="ZGoo"/>
                            <Node Id="(@4 Namespace=N Type=C)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="C" Icon="Microsoft.VisualStudio.Class.Internal" Label="C"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@4 Namespace=N Type=C)" Category="Contains"/>
                            <Link Source="(@1 @3)" Target="(@4 Namespace=N Type=C)" Category="Contains"/>
                            <Link Source="(@4 Namespace=N Type=C)" Target="(@4 Namespace=N Type=C Member=ZBar)" Category="Contains"/>
                            <Link Source="(@4 Namespace=N Type=C)" Target="(@4 Namespace=N Type=C Member=ZGoo)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.vbproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project2.vb"/>
                            <Alias n="3" Uri="File=file:///Z:/Project.vb"/>
                            <Alias n="4" Uri="Assembly=file:///Z:/VisualBasicAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForDottedName1() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class Dog { void Bark() { } }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="D.B"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Type=Dog Member=Bark)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Bark" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark"/>
                            <Node Id="(@3 Type=Dog)" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Dog" Icon="Microsoft.VisualStudio.Class.Internal" Label="Dog"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Type=Dog)" Category="Contains"/>
                            <Link Source="(@3 Type=Dog)" Target="(@3 Type=Dog Member=Bark)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForDottedName2() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class Dog { void Bark() { } }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="C.B"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes/>
                        <Links/>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForDottedName3() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="D.B"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1) Member=Bark)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Bark" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark"/>
                            <Node Id="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Dog&lt;X&gt;" Icon="Microsoft.VisualStudio.Class.Internal" Label="Dog&lt;X&gt;"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Category="Contains"/>
                            <Link Source="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Target="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1) Member=Bark)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchForDottedName4() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                         </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="A.D.B"), GraphContextDirection.Custom)

                AssertSimplifiedGraphIs(
                    outputContext.Graph,
                    <DirectedGraph xmlns="http://schemas.microsoft.com/vs/2009/dgml">
                        <Nodes>
                            <Node Id="(@1 @2)" Category="CodeSchema_ProjectItem" Label="Project.cs"/>
                            <Node Id="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1) Member=Bark)" Category="CodeSchema_Method" CodeSchemaProperty_IsPrivate="True" CommonLabel="Bark" Icon="Microsoft.VisualStudio.Method.Private" Label="Bark"/>
                            <Node Id="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Category="CodeSchema_Class" CodeSchemaProperty_IsInternal="True" CommonLabel="Dog&lt;X&gt;" Icon="Microsoft.VisualStudio.Class.Internal" Label="Dog&lt;X&gt;"/>
                        </Nodes>
                        <Links>
                            <Link Source="(@1 @2)" Target="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Category="Contains"/>
                            <Link Source="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1))" Target="(@3 Namespace=Animal Type=(Name=Dog GenericParameterCount=1) Member=Bark)" Category="Contains"/>
                        </Links>
                        <IdentifierAliases>
                            <Alias n="1" Uri="Assembly=file:///Z:/Project.csproj"/>
                            <Alias n="2" Uri="File=file:///Z:/Project.cs"/>
                            <Alias n="3" Uri="Assembly=file:///Z:/CSharpAssembly1.dll"/>
                        </IdentifierAliases>
                    </DirectedGraph>)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function SearchWithNullFilePathsOnProject() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath=<%= TestWorkspace.NullFilePath %>>
                            <Document FilePath="Z:\SomeVenusDocument.aspx.cs">
                                namespace Animal { class Dog&lt;X&gt; { void Bark() { } } }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim outputContext = Await testState.GetGraphContextAfterQuery(New Graph(), New SearchGraphQuery(searchPattern:="A.D.B"), GraphContextDirection.Custom)

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
