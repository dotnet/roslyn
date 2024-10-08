' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentHighlighting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
    Public Class DocumentHighlightsServiceTests

        <Theory, CombinatorialData>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/441151")>
        Public Async Function TestMultipleLanguagesPassedToAPI(testHost As TestHost) As Task
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
            Using workspace = EditorTestWorkspace.Create(workspaceElement, composition:=EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost))
                Dim position = workspace.DocumentWithCursor.CursorPosition.Value

                Dim solution = workspace.CurrentSolution
                Dim csharpDocument = solution.Projects.Single(Function(p) p.Language = LanguageNames.CSharp).Documents.Single()
                Dim vbDocument = solution.Projects.Single(Function(p) p.Language = LanguageNames.VisualBasic).Documents.Single()
                Dim options = New HighlightingOptions()

                Dim service = csharpDocument.GetLanguageService(Of IDocumentHighlightsService)
                Dim highlights = Await service.GetDocumentHighlightsAsync(
                    csharpDocument, position, ImmutableHashSet.Create(csharpDocument, vbDocument), options, CancellationToken.None)

                AssertEx.Equal(
                    {"Test1.cs: Reference [102..108)"},
                    highlights.Select(Function(h) $"{h.Document.Name}: {String.Join(",", h.HighlightSpans.Select(Function(span) $"{span.Kind} {span.TextSpan}"))}"))
            End Using
        End Function
    End Class
End Namespace
