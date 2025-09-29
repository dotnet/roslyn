' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfFact>
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
            Using workspace = EditorTestWorkspace.Create(definition)
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

        <WpfFact>
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
            Using workspace = EditorTestWorkspace.Create(definition)
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

        <WpfFact>
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

            Using workspace = EditorTestWorkspace.Create(definition)
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/53067")>
        Public Async Function TestLinkedFiles_NamespaceInMetadataAndSource() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
namespace {|Definition:System|}
{
    class C
    {
        void M()
        {
            $$[|System|].Console.WriteLine(0);
        }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)

                Assert.Equal(2, references.Count())
                Assert.Equal("System", references.ElementAt(0).Definition.ToString())
                Assert.Equal("System", references.ElementAt(1).Definition.ToString())
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/53067")>
        Public Async Function TestLinkedFiles_LocalSymbol() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
    class C
    {
        void M()
        {
            int $${|Definition:a|} = 0;
            System.Console.WriteLine([|a|]);
        }
    }
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)

                ' Should find two definitions, one in each file.
                Dim references = (Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)).ToList()
                Assert.Equal(2, references.Count)

                Dim documents = references.Select(Function(r) workspace.CurrentSolution.GetDocument(r.Definition.Locations.Single().SourceTree))
                Assert.Equal(2, documents.Count)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57235")>
        Public Async Function TestLinkedFiles_OverrideMethods_DirectCall_MultiTargetting1() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferencesNetCoreApp="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
class C
{
    public override int $$GetHashCode() => 0;
}

class D
{
    void M()
    {
        new C().[|GetHashCode|]();
    }
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferencesNetStandard20="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                Dim sourceReferences = references.Where(Function(r) r.Definition.Locations(0).IsInSource).ToArray()
                Assert.Equal(2, sourceReferences.Length)
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(0).Definition.ToString())
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(sourceReferences.Select(Function(r) r.Definition.ContainingAssembly.Name), {"CSProj1", "CSProj2"})
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57235")>
        Public Async Function TestLinkedFiles_OverrideMethods_DirectCall_MultiTargetting2() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferencesNetStandard20="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
class C
{
    public override int $$GetHashCode() => 0;
}

class D
{
    void M()
    {
        new C().[|GetHashCode|]();
    }
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferencesNetCoreApp="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                Dim sourceReferences = references.Where(Function(r) r.Definition.Locations(0).IsInSource).ToArray()
                Assert.Equal(2, sourceReferences.Length)
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(0).Definition.ToString())
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(sourceReferences.Select(Function(r) r.Definition.ContainingAssembly.Name), {"CSProj1", "CSProj2"})
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57235")>
        Public Async Function TestLinkedFiles_OverrideMethods_IndirectCall_MultiTargetting1() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferencesNetCoreApp="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
class C
{
    public override int $$GetHashCode() => 0;
}

class D
{
    void M(object o)
    {
        o.[|GetHashCode|]();
    }
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferencesNetStandard20="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                Dim sourceReferences = references.Where(Function(r) r.Definition.Locations(0).IsInSource).ToArray()
                Assert.Equal(2, sourceReferences.Length)
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(0).Definition.ToString())
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(sourceReferences.Select(Function(r) r.Definition.ContainingAssembly.Name), {"CSProj1", "CSProj2"})
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57235")>
        Public Async Function TestLinkedFiles_OverrideMethods_IndirectCall_MultiTargetting2() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferencesNetStandard20="true" AssemblyName="CSProj1">
        <Document FilePath="C.cs"><![CDATA[
class C
{
    public override int $$GetHashCode() => 0;
}

class D
{
    void M(object o)
    {
        o.[|GetHashCode|]();
    }
}
]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferencesNetCoreApp="true" AssemblyName="CSProj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj1" LinkFilePath="C.cs"/>
    </Project>
</Workspace>
            Using workspace = EditorTestWorkspace.Create(definition)
                Dim invocationDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim invocationPosition = invocationDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(invocationDocument.Id)
                Assert.NotNull(document)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, invocationPosition)
                Dim references = Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=Nothing)
                Dim sourceReferences = references.Where(Function(r) r.Definition.Locations(0).IsInSource).ToArray()
                Assert.Equal(2, sourceReferences.Length)
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(0).Definition.ToString())
                Assert.Equal("C.GetHashCode()", sourceReferences.ElementAt(1).Definition.ToString())
                AssertEx.SetEqual(sourceReferences.Select(Function(r) r.Definition.ContainingAssembly.Name), {"CSProj1", "CSProj2"})
            End Using
        End Function
    End Class
End Namespace
