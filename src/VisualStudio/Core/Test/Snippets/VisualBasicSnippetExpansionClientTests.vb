' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
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
        Public Async Function TestAddImport_EmptyDocument() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System"}
            Dim expectedUpdatedCode = <![CDATA[Imports System
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemAtTop() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[Imports System.Bar
Imports First.Alphabetically
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_EmptyDocument_SystemNotSortedToTop() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"First.Alphabetically", "System.Bar"}
            Dim expectedUpdatedCode = <![CDATA[Imports First.Alphabetically
Imports System.Bar
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=False, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewNamespaces() As Task
            Dim originalCode = <![CDATA[Imports A.B.C
Imports D.E.F
]]>.Value
            Dim namespacesToAdd = {"D.E.F", "G.H.I"}
            Dim expectedUpdatedCode = <![CDATA[Imports A.B.C
Imports D.E.F
Imports G.H.I
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact(Skip:="Issue #321"), Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_AddsOnlyNewAliasAndNamespacePairs() As Task
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
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateNamespaceDetectionIgnoresCase() As Task
            Dim originalCode = <![CDATA[Imports A.b.C
]]>.Value
            Dim namespacesToAdd = {"a.B.C", "A.B.c"}
            Dim expectedUpdatedCode = <![CDATA[Imports A.b.C
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace1() As Task
            Dim originalCode = <![CDATA[Imports A = B.C
]]>.Value
            Dim namespacesToAdd = {"A  =        B.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A = B.C
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresWhitespace2() As Task
            Dim originalCode = <![CDATA[Imports A     =  B.C
]]>.Value
            Dim namespacesToAdd = {"A=B.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A     =  B.C
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateAliasNamespacePairDetectionIgnoresCase() As Task
            Dim originalCode = <![CDATA[Imports A = B.C
]]>.Value
            Dim namespacesToAdd = {"a = b.C"}
            Dim expectedUpdatedCode = <![CDATA[Imports A = B.C
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_OnlyFormatNewImports() As Task
            Dim originalCode = <![CDATA[Imports A     =  B.C
Imports G=   H.I
]]>.Value
            Dim namespacesToAdd = {"D        =E.F"}
            Dim expectedUpdatedCode = <![CDATA[Imports A     =  B.C
Imports D = E.F
Imports G=   H.I
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_XmlNamespaceImport() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"<xmlns:db=""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateXmlNamespaceDetectionIgnoresWhitespace1() As Task
            Dim originalCode = <![CDATA[Imports <xmlns:db    = "http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:db=""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db    = "http://example.org/database-two">
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateXmlNamespaceDetectionIgnoresWhitespace2() As Task
            Dim originalCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:db   =          ""http://example.org/database-two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestAddImport_DuplicateXmlNamespaceDetectionIgnoresCase() As Task
            Dim originalCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Dim namespacesToAdd = {"<xmlns:Db=""http://example.org/database-two"">", "<xmlns:db=""http://example.org/Database-Two"">"}
            Dim expectedUpdatedCode = <![CDATA[Imports <xmlns:db="http://example.org/database-two">
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Async Function TestAddImport_BadNamespaceGetsAdded() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"$system"}
            Dim expectedUpdatedCode = <![CDATA[Imports $system
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Async Function TestAddImport_TrailingTriviaIsIncluded() As Task
            Dim originalCode = <![CDATA[]]>.Value
            Dim namespacesToAdd = {"System.Data ' Trivia!"}
            Dim expectedUpdatedCode = <![CDATA[Imports System.Data ' Trivia!
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        <WorkItem(640961)>
        Public Async Function TestAddImport_TrailingTriviaNotUsedInDuplicationDetection() As Task
            Dim originalCode = <![CDATA[Imports System.Data ' Original trivia!
]]>.Value
            Dim namespacesToAdd = {"System.Data ' Different trivia, should not be added!", "System ' Different namespace, should be added"}
            Dim expectedUpdatedCode = <![CDATA[Imports System ' Different namespace, should be added
Imports System.Data ' Original trivia!
]]>.Value
            Await TestSnippetAddImportsAsync(originalCode, namespacesToAdd, placeSystemNamespaceFirst:=True, expectedUpdatedCode:=expectedUpdatedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer() As Task
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

            Await TestFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_FullyInSubjectBuffer2() As Task
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

            Await TestFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_ExpandedIntoSurfaceBuffer() As Task
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

            Await TestFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TestSnippetFormatting_ProjectionBuffer_FullyInSurfaceBuffer() As Task
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

            Await TestFormattingAsync(workspaceXmlWithSubjectBufferDocument, surfaceBufferDocument, expectedSurfaceBuffer)
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

            Using workspace = Await TestWorkspace.CreateAsync(workspaceXml)
                Dim document = workspace.Documents.Single()

                Dim optionService = workspace.Services.GetService(Of IOptionService)()
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

        Private Async Function TestSnippetAddImportsAsync(originalCode As String, namespacesToAdd As String(), placeSystemNamespaceFirst As Boolean, expectedUpdatedCode As String) As Tasks.Task
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

            Using workspace = Await TestWorkspace.CreateAsync(workspaceXml)
                Dim expansionClient = New SnippetExpansionClient(
                    Guids.VisualBasicDebuggerLanguageId,
                    workspace.Documents.Single().GetTextView(),
                    workspace.Documents.Single().GetTextBuffer(),
                    Nothing)

                Dim updatedDocument = expansionClient.AddImports(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    snippetNode,
                    placeSystemNamespaceFirst, CancellationToken.None)

                Assert.Equal(expectedUpdatedCode.Replace(vbLf, vbCrLf),
                             (Await updatedDocument.GetTextAsync()).ToString())
            End Using
        End Function

        Public Async Function TestFormattingAsync(workspaceXmlWithSubjectBufferDocument As XElement, surfaceBufferDocumentXml As XElement, expectedSurfaceBuffer As XElement) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateAsync(workspaceXmlWithSubjectBufferDocument)
                Dim subjectBufferDocument = workspace.Documents.Single()

                Dim surfaceBufferDocument = workspace.CreateProjectionBufferDocument(
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
    End Class
End Namespace
