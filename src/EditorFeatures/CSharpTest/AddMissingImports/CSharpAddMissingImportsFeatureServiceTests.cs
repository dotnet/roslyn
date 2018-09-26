// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.AddMissingImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.AddMissingImports)]
    public class CSharpAddMissingImportsFeatureServiceTests
    {
        private const string LanguageName = LanguageNames.CSharp;

        [Fact]
        public async Task AddMissingImports_DocumentUnchanged_SpanIsNotMissingImports()
        {
            var code = @"
class [|C|]
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await AssertDocumentUnchangedAsync(code).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMissingImports_DocumentChanged_SpanIsMissingImports()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}
";

            var expected = @"
using A;

class C
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await AssertDocumentChangedAsync(code, expected).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMissingImports_DocumentUnchanged_SpanContainsAmbiguousImports()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
}
";

            await AssertDocumentUnchangedAsync(code).ConfigureAwait(false);
        }

        private async Task AssertDocumentUnchangedAsync(string initialMarkup)
        {
            using (var workspace = TestWorkspace.CreateCSharp(initialMarkup))
            {
                var diagnosticAnalyzerService = InitializeDiagnosticAnalyzerService(workspace);

                var addMissingImportsService = new CSharpAddMissingImportsFeatureService(diagnosticAnalyzerService);

                var hostDocument = workspace.Documents.First();
                var documentId = hostDocument.Id;
                var textSpan = hostDocument.SelectedSpans.First();

                var document = workspace.CurrentSolution.GetDocument(documentId);

                var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, CancellationToken.None).ConfigureAwait(false);
                var newDocument = newProject.GetDocument(documentId);

                Assert.Equal(document, newDocument);
            }
        }

        private async Task AssertDocumentChangedAsync(string initialMarkup, string expectedMarkup)
        {
            using (var workspace = TestWorkspace.CreateCSharp(initialMarkup))
            {
                var diagnosticAnalyzerService = InitializeDiagnosticAnalyzerService(workspace);

                var addMissingImportsService = new CSharpAddMissingImportsFeatureService(diagnosticAnalyzerService);

                var hostDocument = workspace.Documents.First();
                var documentId = hostDocument.Id;
                var textSpan = hostDocument.SelectedSpans.First();

                var document = workspace.CurrentSolution.GetDocument(documentId);

                var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, CancellationToken.None).ConfigureAwait(false);
                var newDocument = newProject.GetDocument(documentId);

                Assert.NotEqual(document, newDocument);

                var text = await newDocument.GetTextAsync().ConfigureAwait(false);

                Assert.Equal(expectedMarkup, text.ToString());
            }
        }

        private IDiagnosticAnalyzerService InitializeDiagnosticAnalyzerService(Workspace workspace)
        {
            var diagnosticAnalyzer = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageName);
            var exceptionDiagnosticsSource = new TestHostDiagnosticUpdateSource(workspace);

            var diagnosticAnalyzerService = new TestDiagnosticAnalyzerService(LanguageName, diagnosticAnalyzer, exceptionDiagnosticsSource);
            diagnosticAnalyzerService.CreateIncrementalAnalyzer(workspace);

            return diagnosticAnalyzerService;
        }
    }
}
