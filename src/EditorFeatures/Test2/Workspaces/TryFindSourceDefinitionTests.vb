' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Text
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
    Public Class TryFindSourceDefinitionTests
        Private Function GetProject(snapshot As Solution, assemblyName As String) As Project
            Return snapshot.Projects.Single(Function(p) p.AssemblyName = assemblyName)
        End Function

        <Fact>
        Public Async Function TestFindTypeInCSharpToVisualBasicProject() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
            using N;

            class CSClass
            {
                VBClass field;
            }
    </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
            namespace N
                Public Class VBClass
                End Class
            End Namespace
    </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim snapshot = workspace.CurrentSolution
                Dim Type = (Await GetProject(snapshot, "CSharpAssembly").GetCompilationAsync()).GlobalNamespace.GetTypeMembers("CSClass").Single()

                Dim field = DirectCast(Type.GetMembers("field").Single(), IFieldSymbol)
                Dim fieldType = field.Type

                Assert.Equal(LanguageNames.CSharp, fieldType.Language)
                Assert.True(fieldType.Locations.All(Function(Loc) Loc.IsInMetadata))

                Dim mappedType = Await SymbolFinder.FindSourceDefinitionAsync(fieldType, snapshot, CancellationToken.None)
                Assert.NotNull(mappedType)

                Assert.Equal(LanguageNames.VisualBasic, mappedType.Language)
                Assert.True(mappedType.Locations.All(Function(Loc) Loc.IsInSource))
            End Using
        End Function

        <Fact>
        Public Async Function TestFindTypeInVisualBasicToCSharpProject() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
            imports N

            Public Class VBClass
                Dim field As CSClass
            End Class
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim snapshot = workspace.CurrentSolution
                Dim Type = (Await GetProject(snapshot, "VBAssembly").GetCompilationAsync()).GlobalNamespace.GetTypeMembers("VBClass").Single()

                Dim field = DirectCast(Type.GetMembers("field").Single(), IFieldSymbol)
                Dim fieldType = field.Type

                Assert.Equal(LanguageNames.VisualBasic, fieldType.Language)
                Assert.True(fieldType.Locations.All(Function(Loc) Loc.IsInMetadata))

                Dim mappedType = Await SymbolFinder.FindSourceDefinitionAsync(fieldType, snapshot)
                Assert.NotNull(mappedType)

                Assert.Equal(LanguageNames.CSharp, mappedType.Language)
                Assert.True(mappedType.Locations.All(Function(Loc) Loc.IsInSource))
            End Using
        End Function

        <Fact>
        <WorkItem(1068631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068631")>
        Public Async Function TestFindMethodInVisualBasicToCSharpPortableProject() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferencesPortable="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                    public void M(int i) { }
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim compilation = Await GetProject(workspace.CurrentSolution, "VBAssembly").GetCompilationAsync()
                Dim member = compilation.GlobalNamespace.GetMembers("N").Single().GetTypeMembers("CSClass").Single().GetMembers("M").Single()

                Assert.Equal(LanguageNames.VisualBasic, member.Language)
                Assert.True(member.Locations.All(Function(Loc) Loc.IsInMetadata))

                Dim mappedMember = Await SymbolFinder.FindSourceDefinitionAsync(member, workspace.CurrentSolution)
                Assert.NotNull(mappedMember)

                Assert.Equal(LanguageNames.CSharp, mappedMember.Language)
                Assert.True(mappedMember.Locations.All(Function(Loc) Loc.IsInSource))
            End Using
        End Function

        <Fact>
        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        Public Async Function TestFindMethodInVisualBasicToCSharpProject_RefKindRef() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                    public void M(ref int i) { }
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim compilation = Await GetProject(workspace.CurrentSolution, "VBAssembly").GetCompilationAsync()
                Dim member = compilation.GlobalNamespace.GetMembers("N").Single().GetTypeMembers("CSClass").Single().GetMembers("M").Single()

                Assert.Equal(LanguageNames.VisualBasic, member.Language)
                Assert.True(member.Locations.All(Function(Loc) Loc.IsInMetadata))

                Dim mappedMember = Await SymbolFinder.FindSourceDefinitionAsync(member, workspace.CurrentSolution)
                Assert.NotNull(mappedMember)

                Assert.Equal(LanguageNames.CSharp, mappedMember.Language)
                Assert.True(mappedMember.Locations.All(Function(Loc) Loc.IsInSource))
            End Using
        End Function

        <Fact>
        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        Public Async Function TestFindMethodInVisualBasicToCSharpProject_RefKindOut() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                    public void M(out int i) { }
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim compilation = Await GetProject(workspace.CurrentSolution, "VBAssembly").GetCompilationAsync()
                Dim member = compilation.GlobalNamespace.GetMembers("N").Single().GetTypeMembers("CSClass").Single().GetMembers("M").Single()

                Assert.Equal(LanguageNames.VisualBasic, member.Language)
                Assert.True(member.Locations.All(Function(Loc) Loc.IsInMetadata))

                Dim mappedMember = Await SymbolFinder.FindSourceDefinitionAsync(member, workspace.CurrentSolution)
                Assert.NotNull(mappedMember)

                Assert.Equal(LanguageNames.CSharp, mappedMember.Language)
                Assert.True(mappedMember.Locations.All(Function(Loc) Loc.IsInSource))
            End Using
        End Function


        <Fact>
        Public Async Function TestFindRetargetedClass() As Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferencesPortable="true">
        <Document>
            namespace N
            {
                public class CSClass
                {
                }
            }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim retargetedCompilation = Await GetProject(workspace.CurrentSolution, "CSharpAssembly").GetCompilationAsync()
                Dim originalClass = retargetedCompilation.GlobalNamespace.GetMembers("N").Single().GetTypeMembers("CSClass").Single()
                Dim retargetingCompilation = Await GetProject(workspace.CurrentSolution, "CSharpAssembly2").GetCompilationAsync()
                Dim retargetedClass = retargetingCompilation.GlobalNamespace.GetMembers("N").Single().GetTypeMembers("CSClass").Single()

                ' The retargeted class should not have the same assembly identity as the originating assembly, but
                ' should come through the compilation reference
                Assert.NotEqual(retargetedClass.ContainingAssembly, retargetedCompilation.Assembly)
                Assert.IsAssignableFrom(Of CompilationReference)(retargetingCompilation.GetMetadataReference(retargetedClass.ContainingAssembly))

                Dim mappedMember = Await SymbolFinder.FindSourceDefinitionAsync(retargetedClass, workspace.CurrentSolution)

                Assert.Equal(Of ISymbol)(originalClass, mappedMember)
            End Using
        End Function
    End Class
End Namespace
