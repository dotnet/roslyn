Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    <[UseExportProvider]>
    Public Class DocumentHighlightsServiceTests

        <WorkItem(441151, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/441151")>
        <Fact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestMultipleLanguagesPassedToAPI() As Task
            Dim workspaceElement =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C
                            {
                                $$string Blah()
                                {
                                    return null;
                                }
                            }
                        </Document>
                    </Project>
                    <Project Language="Visual Basic">
                        <Document>
                            Class VBClass
                            End Class
                        </Document>
                    </Project>
                </Workspace>
            Using workspace = TestWorkspace.Create(workspaceElement)
                Dim position = workspace.DocumentWithCursor.CursorPosition.Value

                Dim solution = workspace.CurrentSolution
                Dim csharpDocument = solution.Projects.Single(Function(p) p.Language = LanguageNames.CSharp).Documents.Single()
                Dim vbDocument = solution.Projects.Single(Function(p) p.Language = LanguageNames.VisualBasic).Documents.Single()

                Dim service = csharpDocument.GetLanguageService(Of Microsoft.CodeAnalysis.DocumentHighlighting.IDocumentHighlightsService)
                Await service.GetDocumentHighlightsAsync(
                    csharpDocument, position, ImmutableHashSet.Create(csharpDocument, vbDocument), CancellationToken.None)
            End Using
        End Function
    End Class
End Namespace
