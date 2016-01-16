' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLinkedFiles_Methods() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
class C
{
    void $$M()
    {
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)

                Assert.Equal(2, references.Count())
                Assert.Equal("C.M()", references.ElementAt(0).Definition.ToString())
                Assert.Equal("C.M()", references.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(references.Select(Function(r) r.Definition.ContainingAssembly.Name), {"CSProj1", "CSProj2"})
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLinkedFiles_ClassWithSameSpanAsCompilationUnit() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj1">
        <Document FilePath="C.vb"><![CDATA[
Class $$C
End Class
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj2">
        <Document IsLinkFile="true" LinkAssemblyName="VBProj1" LinkFilePath="C.vb"/>
    </Project>
</Workspace>
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)

                references = references.Where(Function(r) r.Definition.IsKind(SymbolKind.NamedType))

                Assert.Equal(2, references.Count())
                Assert.Equal("C", references.ElementAt(0).Definition.ToString())
                Assert.Equal("C", references.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(references.Select(Function(r) r.Definition.ContainingAssembly.Name), {"VBProj1", "VBProj2"})
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestLinkedFiles_ReferencesBeforeAndAfterRemovingLinkedDocument() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj1">
        <Document FilePath="C.vb"><![CDATA[
Imports System

Class $$C
End Class
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj2">
        <Document IsLinkFile="true" LinkAssemblyName="VBProj1" LinkFilePath="C.vb"/>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value
                Dim linkedDocument = workspace.Documents.Single(Function(d) d.IsLinkFile)

                Dim startingSolution = workspace.CurrentSolution
                Dim updatedSolution = startingSolution.RemoveDocument(linkedDocument.Id)

                ' Original solution should still have a correct snapshot of the linked file state

                Dim document = startingSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                references = references.Where(Function(r) r.Definition.IsKind(SymbolKind.NamedType))

                Assert.Equal(2, references.Count())
                Assert.Equal("C", references.ElementAt(0).Definition.ToString())
                Assert.Equal("C", references.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(references.Select(Function(r) r.Definition.ContainingAssembly.Name), {"VBProj1", "VBProj2"})

                ' The updated solution should reflect the removal of the linked document

                document = updatedSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                references = references.Where(Function(r) r.Definition.IsKind(SymbolKind.NamedType))

                Assert.Equal(1, references.Count())
                Assert.Equal("C", references.ElementAt(0).Definition.ToString())
                AssertEx.SetEqual(references.Select(Function(r) r.Definition.ContainingAssembly.Name), {"VBProj1"})
            End Using
        End Function
    End Class
End Namespace
