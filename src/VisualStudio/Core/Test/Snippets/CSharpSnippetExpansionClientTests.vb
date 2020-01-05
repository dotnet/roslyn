' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    <[UseExportProvider]>
    Public Class CSharpSnippetExpansionClientTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument() As Task
            Dim originalCode = ""
            Dim namespacesToAdd = {"System"}
            Dim expectedUpdatedCode = "using System;
"

            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemAtTop() As Task
            Dim originalCode = ""
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = "using System.Bar;
using First.Alphabetically;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemNotSortedToTop() As Task
            Dim originalCode = ""
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = "using First.Alphabetically;
using System.Bar;
"

            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=False, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewNamespaces() As Task
            Dim originalCode = "using A.B.C;
using D.E.F;
"
            Dim namespacesToAdd = {"D.E.F", "G.H.I"}
            Dim expectedUpdatedCode = "using A.B.C;
using D.E.F;
using G.H.I;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WorkItem(4457, "https://github.com/dotnet/roslyn/issues/4457")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_InsideNamespace() As Task
            Dim originalCode = "
using A;

namespace N
{
    using B;

    class C
    {
        $$
    }
}"
            Dim namespacesToAdd = {"D"}
            Dim expectedUpdatedCode = "
using A;

namespace N
{
    using B;
    using D;

    class C
    {
        
    }
}"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewAliasAndNamespacePairs() As Task
            Dim originalCode = "using A = B.C;
using D = E.F;
using G = H.I;
"
            Dim namespacesToAdd = {"A = E.F", "D = B.C", "G = H.I", "J = K.L"}
            Dim expectedUpdatedCode = "using A = B.C;
using A = E.F;
using D = B.C;
using D = E.F;
using G = H.I;
using J = K.L;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateNamespaceDetectionDoesNotIgnoreCase() As Task
            Dim originalCode = "using A.b.C;
"
            Dim namespacesToAdd = {"a.B.C", "A.B.c"}
            Dim expectedUpdatedCode = "using a.B.C;
using A.b.C;
using A.B.c;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace1() As Task
            Dim originalCode = "using A = B.C;
"
            Dim namespacesToAdd = {"A  =        B.C"}
            Dim expectedUpdatedCode = "using A = B.C;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace2() As Task
            Dim originalCode = "using A     =  B.C;
"
            Dim namespacesToAdd = {"A=B.C"}
            Dim expectedUpdatedCode = "using A     =  B.C;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionDoesNotIgnoreCase() As Task
            Dim originalCode = "using A = B.C;
"
            Dim namespacesToAdd = {"a = b.C"}
            Dim expectedUpdatedCode = "using a = b.C;
using A = B.C;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_OnlyFormatNewImports() As Task
            Dim originalCode = "using A     =  B.C;
using G=   H.I;
"
            Dim namespacesToAdd = {"D        =E.F"}
            Dim expectedUpdatedCode = "using A     =  B.C;
using D = E.F;
using G=   H.I;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_BadNamespaceGetsAdded() As Task
            Dim originalCode = ""
            Dim namespacesToAdd = {"$system"}
            Dim expectedUpdatedCode = "using $system;
"
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub TestSnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
        <Document>class C {
    void M()
    {
        {|S1:for (int x = 0; x &lt; length; x++)
{
        $$ 
}|}
    }</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|} |]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @for (int x = 0; x &lt; length; x++)
        {

        } 
&lt;/div&gt;</SurfaceBuffer>

            TestProjectionFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub TestSnippetFormatting_ProjectionBuffer_ExpandedIntoSurfaceBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
        <Document>class C {
    void M()
    {
        {|S1:for|}
    }</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|} (int x = 0; x &lt; length; x++)
{
        $$
}|]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @for (int x = 0; x &lt; length; x++)
{
        
}
&lt;/div&gt;</SurfaceBuffer>

            TestProjectionFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub TestSnippetFormatting_ProjectionBuffer_FullyInSurfaceBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
        <Document>class C {
    void M()
    {
        {|S1:|}
    }</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|}for (int x = 0; x &lt; length; x++)
{
        $$
}|]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @for (int x = 0; x &lt; length; x++)
{
        
}
&lt;/div&gt;</SurfaceBuffer>

            TestProjectionFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        Public Sub TestSnippetFormatting_TabSize_3()
            TestFormattingWithTabSize(3)
        End Sub

        <WpfTheory, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        <InlineData(3)>
        <InlineData(4)>
        <InlineData(5)>
        Public Sub TestFormattingWithTabSize(tabSize As Integer)
            Dim workspaceXml =
<Workspace>
    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
        <Document>class C {
	void M()
	{
		[|for (int x = 0; x &lt; length; x++)
{
    $$
}|]
	}
}</Document>
    </Project>
</Workspace>

            Dim expectedResult = <Test>class C {
	void M()
	{
		for (int x = 0; x &lt; length; x++)
		{

		}
	}
}</Test>

            Using workspace = TestWorkspace.Create(workspaceXml, openDocuments:=False)
                Dim document = workspace.Documents.Single()

                workspace.Options = workspace.Options _
                    .WithChangedOption(FormattingOptions.UseTabs, document.Project.Language, True) _
                    .WithChangedOption(FormattingOptions.TabSize, document.Project.Language, tabSize) _
                    .WithChangedOption(FormattingOptions.IndentationSize, document.Project.Language, tabSize)

                Dim snippetExpansionClient = New SnippetExpansionClient(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    Guids.CSharpLanguageServiceId,
                    document.GetTextView(),
                    document.GetTextBuffer(),
                    Nothing)

                SnippetExpansionClientTestsHelper.TestFormattingAndCaretPosition(snippetExpansionClient, document, expectedResult, tabSize * 3)
            End Using
        End Sub

        Public Sub TestProjectionFormatting(workspaceXmlWithSubjectBufferDocument As XElement, surfaceBufferDocumentXml As XElement, expectedSurfaceBuffer As XElement)
            Using workspace = TestWorkspace.Create(workspaceXmlWithSubjectBufferDocument)
                Dim subjectBufferDocument = workspace.Documents.Single()

                Dim surfaceBufferDocument = workspace.CreateProjectionBufferDocument(
                    surfaceBufferDocumentXml.NormalizedValue,
                    {subjectBufferDocument},
                    options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim snippetExpansionClient = New SnippetExpansionClient(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    Guids.CSharpLanguageServiceId,
                    surfaceBufferDocument.GetTextView(),
                    subjectBufferDocument.GetTextBuffer(),
                    Nothing)

                SnippetExpansionClientTestsHelper.TestProjectionBuffer(snippetExpansionClient, subjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
            End Using
        End Sub

        Private Async Function TestSnippetAddImportsAsync(
                markupCode As String,
                namespacesToAdd As String(),
                placeSystemNamespaceFirst As Boolean,
                expectedUpdatedCode As String) As Tasks.Task

            Dim originalCode As String = Nothing
            Dim position As Integer?
            MarkupTestFile.GetPosition(markupCode, originalCode, position)

            Dim workspaceXml = <Workspace>
                                   <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                                       <Document><%= originalCode %></Document>
                                   </Project>
                               </Workspace>

            Dim snippetNode = <Snippet>
                                  <Imports>
                                  </Imports>
                              </Snippet>

            For Each namespaceToAdd In namespacesToAdd
                snippetNode.Element("Imports").Add(<Import>
                                                       <Namespace><%= namespaceToAdd %></Namespace>
                                                   </Import>)
            Next

            Using workspace = TestWorkspace.CreateCSharp(originalCode)
                Dim expansionClient = New SnippetExpansionClient(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    Guids.VisualBasicDebuggerLanguageId,
                    workspace.Documents.Single().GetTextView(),
                    workspace.Documents.Single().GetTextBuffer(),
                    Nothing)

                Dim updatedDocument = expansionClient.AddImports(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    If(position, 0),
                    snippetNode,
                    placeSystemNamespaceFirst, CancellationToken.None)

                Assert.Equal(expectedUpdatedCode,
                             (Await updatedDocument.GetTextAsync()).ToString())
            End Using
        End Function
    End Class
End Namespace
