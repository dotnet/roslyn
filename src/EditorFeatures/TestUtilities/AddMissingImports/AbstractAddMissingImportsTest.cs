// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.AddMissingImports
{
    [UseExportProvider]
    public abstract class AbstractAddMissingImportsFeatureServiceTest
    {
        private string LanguageName { get; }

        protected AbstractAddMissingImportsFeatureServiceTest(string languageName)
        {
            LanguageName = languageName;
        }

        protected abstract ExportProvider CreateExportProvider();

        private TestWorkspace CreateWorkspace(string initialMarkup)
        {
            var exportProvider = CreateExportProvider();
            var workspace = LanguageName == LanguageNames.CSharp
                ? TestWorkspace.CreateCSharp(initialMarkup, exportProvider: exportProvider)
                : TestWorkspace.CreateVisualBasic(initialMarkup, exportProvider: exportProvider);

            var diagnosticAnalyzerService = (DiagnosticAnalyzerService)exportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            diagnosticAnalyzerService.CreateIncrementalAnalyzer(workspace);

            return workspace;
        }

        protected async Task AssertDocumentUnchangedAsync(string initialMarkup)
        {
            using (var workspace = CreateWorkspace(initialMarkup))
            {
                var (document, newDocument) = await GetDocumentBeforeAndAfterAddMissingImportsAsync(workspace);

                var textChanges = await newDocument.GetTextChangesAsync(document);
                Assert.False(textChanges.Any());
            }
        }

        protected async Task AssertDocumentChangedAsync(string initialMarkup, string expectedMarkup)
        {
            using (var workspace = CreateWorkspace(initialMarkup))
            {
                var (document, newDocument) = await GetDocumentBeforeAndAfterAddMissingImportsAsync(workspace);

                var textChanges = await newDocument.GetTextChangesAsync(document);
                Assert.True(textChanges.Any());

                var text = await newDocument.GetTextAsync();

                Assert.Equal(expectedMarkup, text.ToString());
            }
        }

        private async Task<(Document, Document)> GetDocumentBeforeAndAfterAddMissingImportsAsync(TestWorkspace workspace)
        {
            var hostDocument = workspace.Documents.First();
            var documentId = hostDocument.Id;
            var textSpan = hostDocument.SelectedSpans.First();

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var addMissingImportsService = document.Project.LanguageServices.GetService<IAddMissingImportsFeatureService>();

            var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, CancellationToken.None);
            var newDocument = newProject.GetDocument(documentId);

            return (document, newDocument);
        }
    }
}
