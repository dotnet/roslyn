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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    [UseExportProvider]
    public class DiagnosticAnalyzerQuickInfoSourceTests
    {
        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DisabledWarningIsShownInQuickInfoWithCodeDetails()
        {
            await TestInMethodAsync(
@"
#pragma warning disable CS0219$$
            var i = 0;
#pragma warning restore CS0219
", @"Die Variable ""i"" ist zugewiesen, ihr Wert wird aber nie verwendet.", TextSpan.FromBounds(71, 72));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task DisabledWarningNotExistingInCodeIsDisplayedByTitleWithoutCodeDetails()
        {
            await TestInMethodAsync(
@"
#pragma warning disable CS0219$$
", @"Variable ist zugewiesen, der Wert wird jedoch niemals verwendet");
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task WarningBeforeDisablePragmaIsNotConsidered()
        {
            await TestInMethodAsync(
@"
        var i1 = 0;
#pragma warning disable CS0219$$
        var i2 = 0;
", @"Die Variable ""i2"" ist zugewiesen, ihr Wert wird aber nie verwendet.", TextSpan.FromBounds(88, 90));
        }

        [WorkItem(46604, "https://github.com/dotnet/roslyn/issues/46604")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("#pragma warning disable $$CS0162", @"Unerreichbarer Code wurde entdeckt.", 80, 83)]
        [InlineData("#pragma warning disable $$CS0162, CS0219", @"Unerreichbarer Code wurde entdeckt.", 88, 91)]
        [InlineData("#pragma warning disable $$CS0219", @"Die Variable ""i"" ist zugewiesen, ihr Wert wird aber nie verwendet.", 84, 85)]
        [InlineData("#pragma warning disable CS0162, $$CS0219", @"Die Variable ""i"" ist zugewiesen, ihr Wert wird aber nie verwendet.", 92, 93)]
        [InlineData("#pragma warning $$disable CS0162, CS0219", @"Unerreichbarer Code wurde entdeckt.", 88, 91)]
        [InlineData("#pragma warning $$disable CS0219, CS0162", @"Die Variable ""i"" ist zugewiesen, ihr Wert wird aber nie verwendet.", 92, 93)]
        public async Task MultipleWarningsAreDisplayedDependingOnCursorPosition(string pragma, string description, int relatedSpandStart, int relatedSpanEnd)
        {
            await TestInMethodAsync(
@$"
{pragma}
        return;
        var i = 0;
", description, TextSpan.FromBounds(relatedSpandStart, relatedSpanEnd));
        }

        protected static async Task AssertContentIsAsync(TestWorkspace workspace, Document document, int position, string expectedDescription,
            ImmutableArray<TextSpan> relatedSpans)
        {
            var info = await GetQuickinfo(workspace, document, position);
            var description = info?.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
            Assert.Equal(expectedDescription, description.Text);
            Assert.Collection(relatedSpans,
                info.RelatedSpans.Select(actualSpan => new Action<TextSpan>(expectedSpan => Assert.Equal(expectedSpan, actualSpan))).ToArray());
        }

        private static async Task<QuickInfoItem> GetQuickinfo(TestWorkspace workspace, Document document, int position)
        {
            var diagnosticAnalyzerService = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var sut = new CSharpDiagnosticAnalyzerQuickInfoProvider(diagnosticAnalyzerService);
            var info = await sut.GetQuickInfoAsync(new QuickInfoContext(document, position, CancellationToken.None));
            return info;
        }

        protected static async Task AssertNoContentAsync(TestWorkspace workspace, Document document, int position)
        {
            var info = await GetQuickinfo(workspace, document, position);
            Assert.Null(info);
        }

        protected static async Task TestAsync(
            string code,
            string expectedDescription,
            ImmutableArray<TextSpan> relatedSpans,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, parseOptions);
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var diagnosticAnalyzerService = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var analyzer = (diagnosticAnalyzerService as IIncrementalAnalyzerProvider).CreateIncrementalAnalyzer(workspace);
            await analyzer.AnalyzeProjectAsync(workspace.CurrentSolution.Projects.Single(), semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);

            var testDocument = workspace.Documents.Single();
            var position = testDocument.CursorPosition.Value;
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (string.IsNullOrEmpty(expectedDescription))
            {
                await AssertNoContentAsync(workspace, document, position);
            }
            else
            {
                await AssertContentIsAsync(workspace, document, position, expectedDescription, relatedSpans);
            }
        }

        protected static Task TestInClassAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
            => TestAsync(
@"class C
{" + code + "}", expectedDescription, relatedSpans.ToImmutableArray());

        protected static Task TestInMethodAsync(string code, string expectedDescription, params TextSpan[] relatedSpans)
            => TestInClassAsync(
@"void M()
{" + code + "}", expectedDescription, relatedSpans);
    }
}
