// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.AddMissingImports)]
    public class AddMissingImportsRefactoringProviderTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            var testWorkspace = (TestWorkspace)workspace;
            var pasteTrackingService = testWorkspace.ExportProvider.GetExportedValue<PasteTrackingService>();
            return new AddMissingImportsRefactoringProvider(pasteTrackingService);
        }

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            var workspace = TestWorkspace.CreateCSharp(initialMarkup, exportProvider: GetExportProvider());

            var diagnosticAnalyzerService = (DiagnosticAnalyzerService)workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            diagnosticAnalyzerService.CreateIncrementalAnalyzer(workspace);

            // Treat the span being tested as the pasted span
            var hostDocument = workspace.Documents.First();
            var pastedTextSpan = hostDocument.SelectedSpans.FirstOrDefault();

            if (!pastedTextSpan.IsEmpty)
            {
                var pasteTrackingService = workspace.ExportProvider.GetExportedValue<PasteTrackingService>();
                pasteTrackingService.RegisterPastedTextSpan(hostDocument.GetTextView(), hostDocument.TextBuffer, pastedTextSpan);
            }

            return workspace;
        }

        [WpfFact]
        public async Task AddMissingImports_Added_WhenPasteIsMissingImports()
        {
            var code = @"
class C
{
    static void Main()
    {
        var a = [|new Dictionary<string, object>();|]
    }
}";

            var expected = @"
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var a = new Dictionary<string, object>();
    }
}";

            await TestInRegularAndScriptAsync(code, expected).ConfigureAwait(false);
        }

        [WpfFact]
        public async Task AddMissingImports_Missing_WhenNoPastedSpan()
        {
            var code = @"
class C
{
    static void Main()
    {
        var a = new [||]Dictionary<string, object>();
    }
}";

            await TestMissingInRegularAndScriptAsync(code).ConfigureAwait(false);
        }

        [WpfFact]
        public async Task AddMissingImports_Missing_WhenPasteIsNotMissingImports()
        {
            var code = @"
using System.Collections.Generic;

class C
{
    static void Main()
    {
        var a = [|new Dictionary<string, object>();|]
    }
}";

            await TestMissingInRegularAndScriptAsync(code).ConfigureAwait(false);
        }

        private static Lazy<IExportProviderFactory> s_exportProviderFactory = new Lazy<IExportProviderFactory>(() =>
        {
            var catalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                .WithoutPartsOfType(typeof(IWorkspaceDiagnosticAnalyzerProviderService))
                .WithPart(typeof(CSharpCompilerDiagnosticAnalyzerProviderService));

            return ExportProviderCache.GetOrCreateExportProviderFactory(catalog);
        });

        private static ExportProvider GetExportProvider() => s_exportProviderFactory.Value.CreateExportProvider();
    }
}
