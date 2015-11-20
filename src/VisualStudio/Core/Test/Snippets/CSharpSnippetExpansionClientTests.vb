' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Public Class CSharpSnippetExpansionClientTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System"}
            Dim expectedUpdatedCode = <![CDATA[using System;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemAtTop() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[using System.Bar;
using First.Alphabetically;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemNotSortedToTop() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[using First.Alphabetically;
using System.Bar;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=False, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewNamespaces() As Task
            Dim originalCode = <![CDATA[using A.B.C;
using D.E.F;
]]>.Value
            Dim namespacesToAdd = {"D.E.F", "G.H.I"}
            Dim expectedUpdatedCode = <![CDATA[using A.B.C;
using D.E.F;
using G.H.I;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewAliasAndNamespacePairs() As Task
            Dim originalCode = <![CDATA[using A = B.C;
using D = E.F;
using G = H.I;
]]>.Value
            Dim namespacesToAdd = {"A = E.F", "D = B.C", "G = H.I", "J = K.L"}
            Dim expectedUpdatedCode = <![CDATA[using A = B.C;
using A = E.F;
using D = B.C;
using D = E.F;
using G = H.I;
using J = K.L;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateNamespaceDetectionDoesNotIgnoreCase() As Task
            Dim originalCode = <![CDATA[using A.b.C;
]]>.Value
            Dim namespacesToAdd = {"a.B.C", "A.B.c"}
            Dim expectedUpdatedCode = <![CDATA[using a.B.C;
using A.b.C;
using A.B.c;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace1() As Task
            Dim originalCode = <![CDATA[using A = B.C;
]]>.Value
            Dim namespacesToAdd = {"A  =        B.C"}
            Dim expectedUpdatedCode = <![CDATA[using A = B.C;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace2() As Task
            Dim originalCode = <![CDATA[using A     =  B.C;
]]>.Value
            Dim namespacesToAdd = {"A=B.C"}
            Dim expectedUpdatedCode = <![CDATA[using A     =  B.C;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionDoesNotIgnoreCase() As Task
            Dim originalCode = <![CDATA[using A = B.C;
]]>.Value
            Dim namespacesToAdd = {"a = b.C"}
            Dim expectedUpdatedCode = <![CDATA[using a = b.C;
using A = B.C;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_OnlyFormatNewImports() As Task
            Dim originalCode = <![CDATA[using A     =  B.C;
using G=   H.I;
]]>.Value
            Dim namespacesToAdd = {"D        =E.F"}
            Dim expectedUpdatedCode = <![CDATA[using A     =  B.C;
using D = E.F;
using G=   H.I;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_BadNamespaceGetsAdded() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"$system"}
            Dim expectedUpdatedCode = <![CDATA[using $system;
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer() As Task
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

            Await TestProjectionFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_ExpandedIntoSurfaceBuffer() As Task
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

            Await TestProjectionFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_FullyInSurfaceBuffer() As Task
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

            Await TestProjectionFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_TabSize_3() As Task
            Await TestFormattingWithTabSizeAsync(3)
        End Function

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_TabSize_4() As Task
            Await TestFormattingWithTabSizeAsync(4)
        End Function

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_TabSize_5() As Task
            Await TestFormattingWithTabSizeAsync(5)
        End Function

        Public Async Function TestFormattingWithTabSizeAsync(tabSize As Integer) As Tasks.Task
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

            Using testWorkspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim document = testWorkspace.Documents.Single()

                Dim optionService = testWorkspace.Services.GetService(Of IOptionService)()
                Dim optionSet = optionService.GetOptions()
                optionSet = optionSet.WithChangedOption(FormattingOptions.UseTabs, document.Project.Language, True)
                optionSet = optionSet.WithChangedOption(FormattingOptions.TabSize, document.Project.Language, tabSize)
                optionSet = optionSet.WithChangedOption(FormattingOptions.IndentationSize, document.Project.Language, tabSize)
                optionService.SetOptions(optionSet)

                Dim snippetExpansionClient = New SnippetExpansionClient(
                    Guids.CSharpLanguageServiceId,
                    document.GetTextView(),
                    document.TextBuffer,
                    Nothing)

                SnippetExpansionClientTestsHelper.TestFormattingAndCaretPosition(snippetExpansionClient, document, expectedResult, tabSize * 3)
            End Using
        End Function

        Public Async Function TestProjectionFormattingAsync(workspaceXmlWithSubjectBufferDocument As XElement, surfaceBufferDocumentXml As XElement, expectedSurfaceBuffer As XElement) As Tasks.Task
            Using testWorkspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXmlWithSubjectBufferDocument)
                Dim subjectBufferDocument = testWorkspace.Documents.Single()

                Dim surfaceBufferDocument = testWorkspace.CreateProjectionBufferDocument(
                    surfaceBufferDocumentXml.NormalizedValue,
                    {subjectBufferDocument},
                    LanguageNames.VisualBasic,
                    options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim snippetExpansionClient = New SnippetExpansionClient(
                    Guids.CSharpLanguageServiceId,
                    surfaceBufferDocument.GetTextView(),
                    subjectBufferDocument.TextBuffer,
                    Nothing)

                SnippetExpansionClientTestsHelper.TestProjectionBuffer(snippetExpansionClient, subjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
            End Using
        End Function

        Private Async Function TestSnippetAddImportsAsync(originalCode As String, namespacesToAdd As String(), placeSystemNamespaceFirst As Boolean, expectedUpdatedCode As String) As Tasks.Task
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

            Using testWorkspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim expansionClient = New SnippetExpansionClient(
                    Guids.VisualBasicDebuggerLanguageId,
                    testWorkspace.Documents.Single().GetTextView(),
                    testWorkspace.Documents.Single().GetTextBuffer(),
                    Nothing)

                Dim updatedDocument = expansionClient.AddImports(
                    testWorkspace.CurrentSolution.Projects.Single().Documents.Single(),
                    snippetNode,
                    placeSystemNamespaceFirst, CancellationToken.None)

                Assert.Equal(expectedUpdatedCode.Replace(vbLf, vbCrLf),
                             (Await updatedDocument.GetTextAsync(CancellationToken.None)).ToString())
            End Using
        End Function

    End Class
End Namespace
