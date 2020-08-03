﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
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
            Using workspace = TestWorkspace.Create(definition)
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
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
            Using workspace = TestWorkspace.Create(definition)
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

            Using workspace = TestWorkspace.Create(definition)
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Function TestLinkedFiles_LinkedFilesWithSameAssemblyNameNoReferences(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.1">
        <Document FilePath="C.cs"><![CDATA[
class {|Definition:$$C|}
{
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.2">
        <Document IsLinkFile="true" LinkProjectName="CSProj.1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>

            Return TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Function TestLinkedFiles_LinkedFilesWithSameAssemblyNameWithReferences(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.1">
        <Document FilePath="C.cs"><![CDATA[
public class {|Definition:$$C|}
{
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.2">
        <Document IsLinkFile="true" LinkProjectName="CSProj.1" LinkFilePath="C.cs"/>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="ReferencingProject1">
        <ProjectReference>CSProj.1</ProjectReference>
        <Document FilePath="D.cs"><![CDATA[
public class D : [|$$C|]
{
}]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="ReferencingProject2">
        <ProjectReference>CSProj.2</ProjectReference>
        <Document FilePath="E.cs"><![CDATA[
public class D : [|$$C|]
{
}]]>
        </Document>
    </Project>
</Workspace>

            Return TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
