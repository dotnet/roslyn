' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
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
        Public Sub AddImport_EmptyDocument()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System"}
            Dim expectedUpdatedCode = <![CDATA[using System;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_EmptyDocument_SystemAtTop()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[using System.Bar;
using First.Alphabetically;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_EmptyDocument_SystemNotSortedToTop()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[using First.Alphabetically;
using System.Bar;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=False, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_AddsOnlyNewNamespaces()
            Dim originalCode = <![CDATA[using A.B.C;
using D.E.F;
]]>.Value
            Dim namespacesToAdd = {"D.E.F", "G.H.I"}
            Dim expectedUpdatedCode = <![CDATA[using A.B.C;
using D.E.F;
using G.H.I;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_AddsOnlyNewAliasAndNamespacePairs()
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
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateNamespaceDetectionDoesNotIgnoreCase()
            Dim originalCode = <![CDATA[using A.b.C;
]]>.Value
            Dim namespacesToAdd = {"a.B.C", "A.B.c"}
            Dim expectedUpdatedCode = <![CDATA[using a.B.C;
using A.b.C;
using A.B.c;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace1()
            Dim originalCode = <![CDATA[using A = B.C;
]]>.Value
            Dim namespacesToAdd = {"A  =        B.C"}
            Dim expectedUpdatedCode = <![CDATA[using A = B.C;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace2()
            Dim originalCode = <![CDATA[using A     =  B.C;
]]>.Value
            Dim namespacesToAdd = {"A=B.C"}
            Dim expectedUpdatedCode = <![CDATA[using A     =  B.C;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionDoesNotIgnoreCase()
            Dim originalCode = <![CDATA[using A = B.C;
]]>.Value
            Dim namespacesToAdd = {"a = b.C"}
            Dim expectedUpdatedCode = <![CDATA[using a = b.C;
using A = B.C;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_OnlyFormatNewImports()
            Dim originalCode = <![CDATA[using A     =  B.C;
using G=   H.I;
]]>.Value
            Dim namespacesToAdd = {"D        =E.F"}
            Dim expectedUpdatedCode = <![CDATA[using A     =  B.C;
using D = E.F;
using G=   H.I;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_BadNamespaceGetsAdded()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"$system"}
            Dim expectedUpdatedCode = <![CDATA[using $system;
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer()
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
        Public Sub SnippetFormatting_ProjectionBuffer_ExpandedIntoSurfaceBuffer()
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
        Public Sub SnippetFormatting_ProjectionBuffer_FullyInSurfaceBuffer()
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

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_TabSize_3()
            TestFormattingWithTabSize(3)
        End Sub

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_TabSize_4()
            TestFormattingWithTabSize(4)
        End Sub

        <WpfFact, WorkItem(4652, "https://github.com/dotnet/roslyn/issues/4652")>
        <Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_TabSize_5()
            TestFormattingWithTabSize(5)
        End Sub

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

            Using testWorkspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
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
        End Sub

        Public Sub TestProjectionFormatting(workspaceXmlWithSubjectBufferDocument As XElement, surfaceBufferDocumentXml As XElement, expectedSurfaceBuffer As XElement)
            Using testWorkspace = TestWorkspaceFactory.CreateWorkspace(workspaceXmlWithSubjectBufferDocument)
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
        End Sub

        Private Sub TestSnippetAddImports(originalCode As String, namespacesToAdd As String(), placeSystemNamespaceFirst As Boolean, expectedUpdatedCode As String)
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

            Using testWorkspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
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
                             updatedDocument.GetTextAsync(CancellationToken.None).Result.ToString())
            End Using
        End Sub

    End Class
End Namespace
