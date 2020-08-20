// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public class DiagnosticAnalyzerQuickInfoSourceTests : AbstractQuickInfoSourceTests
    {
        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task PragmaWarning()
        {
            await TestInClassAsync(
@"
        static void M()
        {
#pragma warning disable CS0219$$
            var i = 0;
#pragma warning restore CS0219
        }
", @"test1.cs(4,17): warning CS0168: Die Variable ""i"" ist deklariert, wird aber nie verwendet.");
        }

        protected override async Task AssertContentIsAsync(TestWorkspace workspace, Document document, int position, string expectedContent, string expectedDocumentationComment = null)
        {
            var diagnosticAnalyzerService = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var analyzer = (diagnosticAnalyzerService as IIncrementalAnalyzerProvider).CreateIncrementalAnalyzer(workspace);
            await analyzer.AnalyzeProjectAsync(document.Project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);
            var sut = new CSharpDiagnosticAnalyzerQuickInfoProvider(diagnosticAnalyzerService);
            var info = await sut.GetQuickInfoAsync(new QuickInfoContext(document, position, CancellationToken.None));
        }

        protected override Task AssertNoContentAsync(TestWorkspace workspace, Document document, int position)
        {
            throw new NotImplementedException();
        }

        protected override async Task TestAsync(
            string code,
            string expectedContent,
            string expectedDocumentationComment = null,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, parseOptions);
            var testDocument = workspace.Documents.Single();
            var position = testDocument.CursorPosition.Value;
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (string.IsNullOrEmpty(expectedContent))
            {
                await AssertNoContentAsync(workspace, document, position);
            }
            else
            {
                await AssertContentIsAsync(workspace, document, position, expectedContent, expectedDocumentationComment);
            }
        }

        protected override Task TestInClassAsync(string code, string expectedContent, string expectedDocumentationComment = null)
            => TestAsync(
@"class C
{" + code + "}", expectedContent, expectedDocumentationComment);

        protected override Task TestInMethodAsync(string code, string expectedContent, string expectedDocumentationComment = null)
            => TestInClassAsync(
@"void M()
{" + code + "}", expectedContent, expectedDocumentationComment);

        protected override Task TestInScriptAsync(string code, string expectedContent, string expectedDocumentationComment = null)
            => TestAsync(code, expectedContent, expectedContent, Options.Script);
    }
}
