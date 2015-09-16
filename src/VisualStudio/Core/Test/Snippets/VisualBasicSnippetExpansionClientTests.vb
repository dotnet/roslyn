' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Public Class VisualBasicSnippetExpansionClientTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_EmptyDocument()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System"}
            Dim expectedUpdatedCode = <![CDATA[Imports System
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_EmptyDocument_SystemAtTop()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[Imports System.Bar
Imports First.Alphabetically
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_EmptyDocument_SystemNotSortedToTop()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[Imports First.Alphabetically
Imports System.Bar
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=False, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_AddsOnlyNewNamespaces()
            Dim originalCode = <![CDATA[Imports A.B.C
Imports D.E.F
]]>.Value
            Dim namespacesToAdd = {"D.E.F", "G.H.I"}
            Dim expectedUpdatedCode = <![CDATA[Imports A.B.C
Imports D.E.F
Imports G.H.I
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact(Skip:="Issue #321"), Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_AddsOnlyNewAliasAndNamespacePairs()
            Dim originalCode = <![CDATA[Imports A = B.C
Imports D = E.F
Imports G = H.I
]]>.Value
            Dim namespacesToAdd = {"A = E.F", "D = B.C", "G = H.I", "J = K.L"}
            Dim expectedUpdatedCode = <![CDATA[Imports A = B.C
Imports A = E.F
Imports D = B.C
Imports D = E.F
Imports G = H.I
Imports J = K.L
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateNamespaceDetectionIgnoresCase()
            Dim originalCode = <![CDATA[Imports A.b.C
]]>.Value
            Dim namespacesToAdd = {"a.B.C", "A.B.c"}
            Dim expectedUpdatedCode = <![CDATA[Imports A.b.C
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace1()
            Dim originalCode = <![CDATA[Imports A = B.C
]]>.Value
            Dim namespacesToAdd = {"A  =        B.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A = B.C
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace2()
            Dim originalCode = <![CDATA[Imports A     =  B.C
]]>.Value
            Dim namespacesToAdd = {"A=B.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A     =  B.C
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateAliasNamespacePairDetectionIgnoresCase()
            Dim originalCode = <![CDATA[Imports A = B.C
]]>.Value
            Dim namespacesToAdd = {"a = b.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A = B.C
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_OnlyFormatNewImports()
            Dim originalCode = <![CDATA[Imports A     =  B.C
Imports G=   H.I
]]>.Value
            Dim namespacesToAdd = {"D        =E.F"}
            Dim expectedUpdatedCode = <![CDATA[Imports A     =  B.C
Imports D = E.F
Imports G=   H.I
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_XmlNamespaceImport()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"<xmlns:db=""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateXmlNamespaceDetectionIgnoresWhitespace1()
            Dim originalCode = <![CDATA[Imports <xmlns:db    = "http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:db=""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db    = "http://example.org/database-two">
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateXmlNamespaceDetectionIgnoresWhitespace2()
            Dim originalCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:db   =          ""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub AddImport_DuplicateXmlNamespaceDetectionIgnoresCase()
            Dim originalCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:Db=""http://example.org/database-two"">", "<xmlns:db=""http://example.org/Database-Two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Sub AddImport_BadNamespaceGetsAdded()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"$system"}
            Dim expectedUpdatedCode = <![CDATA[Imports $system
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Sub AddImport_TrailingTriviaIsIncluded()
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System.Data ' Trivia!"}
            Dim expectedUpdatedCode = <![CDATA[Imports System.Data ' Trivia!
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Sub AddImport_TrailingTriviaNotUsedInDuplicationDetection()
            Dim originalCode = <![CDATA[Imports System.Data ' Original trivia!
]]>.Value
            Dim namespacesToAdd = {"System.Data ' Different trivia, should not be added!", "System ' Different namespace, should be added"}
            Dim expectedUpdatedCode = <![CDATA[Imports System ' Different namespace, should be added
Imports System.Data ' Original trivia!
]]>.Value
            TestSnippetAddImports(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
        <Document>Class C
    Sub M()
        @{|S1:For index = 1 to length
        $$
Next|}
    End Sub
End Class</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|} |]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @For index = 1 to length

Next 
&lt;/div&gt;</SurfaceBuffer>

            TestFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer2()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
        <Document>Class C
    Sub M()
        @{|S1:For index = 1 to length
For index2 = 1 to length
        $$
Next
Next|}
    End Sub
End Class</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|} |]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @For index = 1 to length
For index2 = 1 to length

        Next
        Next 
&lt;/div&gt;</SurfaceBuffer>

            TestFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_ProjectionBuffer_ExpandedIntoSurfaceBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
        <Document>Class C
    Sub M()
        @{|S1:For|}
    End Sub
End Class</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|} index = 1 to length
        $$
Next|]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @For index = 1 to length
        
Next
&lt;/div&gt;</SurfaceBuffer>

            TestFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetFormatting_ProjectionBuffer_FullyInSurfaceBuffer()
            Dim workspaceXmlWithSubjectBufferDocument =
<Workspace>
    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
        <Document>Class C
    Sub M()
        @{|S1:|}
    End Sub
End Class</Document>
    </Project>
</Workspace>

            Dim surfaceBufferDocument = <Document>&lt;div&gt;
    @[|{|S1:|}For index = 1 to length
        $$
Next|]
&lt;/div&gt;</Document>

            Dim expectedSurfaceBuffer = <SurfaceBuffer>&lt;div&gt;
    @For index = 1 to length
        
Next
&lt;/div&gt;</SurfaceBuffer>

            TestFormatting(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
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
    <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
        <Document>Class C
	Sub M()
		[|For index = 1 To 10
    $$
Next|]
	End Sub
End Class</Document>
    </Project>
</Workspace>

            Dim expectedResult = <Test>Class C
	Sub M()
		For index = 1 To 10

		Next
	End Sub
End Class</Test>

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

        Private Sub TestSnippetAddImports(originalCode As String, namespacesToAdd As String(), placeSystemNamespaceFirst As Boolean, expectedUpdatedCode As String)
            Dim workspaceXml = <Workspace>
                                   <Project Language=<%= LanguageNames.VisualBasic %> CommonReferences="true">
                                       <CompilationOptions/>
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

        Public Sub TestFormatting(workspaceXmlWithSubjectBufferDocument As XElement, surfaceBufferDocumentXml As XElement, expectedSurfaceBuffer As XElement)
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
    End Class
End Namespace
