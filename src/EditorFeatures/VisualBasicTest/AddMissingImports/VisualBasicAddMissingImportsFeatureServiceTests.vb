' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.AddMissingImports

Namespace Microsoft.CodeAnalysis.AddMissingImports

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.AddMissingImports)>
    Public Class VisualBasicAddMissingImportsFeatureServiceTests
        Private Const LanguageName = LanguageNames.VisualBasic

        <Fact>
        Public Async Function AddMissingImports_DocumentUnchanged_SpanIsNotMissingImports() As Task
            Dim code = "
Class [|C|]
    Dim foo As D
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentUnchangedAsync(code).ConfigureAwait(False)
        End Function

        <Fact>
        Public Async Function AddMissingImports_DocumentChanged_SpanIsMissingImports() As Task
            Dim code = "
Class C
    Dim foo As [|D|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Dim expected = "
Imports A

Class C
    Dim foo As D
End Class

Namespace A
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentChangedAsync(code, expected).ConfigureAwait(False)
        End Function

        <Fact>
        Public Async Function AddMissingImports_DocumentUnchanged_SpanContainsAmbiguousImports() As Task
            Dim code = "
Class C
    Dim foo As [|D|]
End Class

Namespace A
    Public Class D
    End Class
End Namespace

Namespace B
    Public Class D
    End Class
End Namespace
"

            Await AssertDocumentUnchangedAsync(code).ConfigureAwait(False)
        End Function

        Private Async Function AssertDocumentUnchangedAsync(initialMarkup As String) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(initialMarkup)

                Dim diagnosticAnalyzerService = InitializeDiagnosticAnalyzerService(workspace)

                Dim addMissingImportsService = New VisualBasicAddMissingImportsFeatureService(diagnosticAnalyzerService)

                Dim hostDocument = workspace.Documents.First()
                Dim documentId = hostDocument.Id
                Dim textSpan = hostDocument.SelectedSpans.First()

                Dim document = workspace.CurrentSolution.GetDocument(documentId)

                Dim newProject = Await addMissingImportsService.AddMissingImportsAsync(document, textSpan, CancellationToken.None).ConfigureAwait(False)
                Dim newDocument = newProject.GetDocument(documentId)

                Assert.Equal(document, newDocument)
            End Using
        End Function

        Private Async Function AssertDocumentChangedAsync(initialMarkup As String, expectedMarkup As String) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(initialMarkup)

                Dim diagnosticAnalyzerService = InitializeDiagnosticAnalyzerService(workspace)

                Dim addMissingImportsService = New VisualBasicAddMissingImportsFeatureService(diagnosticAnalyzerService)

                Dim hostDocument = workspace.Documents.First()
                Dim documentId = hostDocument.Id
                Dim textSpan = hostDocument.SelectedSpans.First()

                Dim document = workspace.CurrentSolution.GetDocument(documentId)

                Dim newProject = Await addMissingImportsService.AddMissingImportsAsync(document, textSpan, CancellationToken.None).ConfigureAwait(False)
                Dim newDocument = newProject.GetDocument(documentId)

                Assert.NotEqual(document, newDocument)

                Dim Text = Await newDocument.GetTextAsync().ConfigureAwait(False)

                Assert.Equal(expectedMarkup, Text.ToString())
            End Using
        End Function

        Private Function InitializeDiagnosticAnalyzerService(workspace As Workspace) As IDiagnosticAnalyzerService
            Dim diagnosticAnalyzer = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageName)
            Dim exceptionDiagnosticsSource = New TestHostDiagnosticUpdateSource(workspace)

            Dim diagnosticAnalyzerService = New TestDiagnosticAnalyzerService(LanguageName, diagnosticAnalyzer, exceptionDiagnosticsSource)
            diagnosticAnalyzerService.CreateIncrementalAnalyzer(workspace)

            Return diagnosticAnalyzerService
        End Function
    End Class
End Namespace

