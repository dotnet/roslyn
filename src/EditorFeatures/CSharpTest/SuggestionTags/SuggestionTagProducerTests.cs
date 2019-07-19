// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SuggestionTags
{
    [UseExportProvider]
    public class SuggestionTagProducerTests
    {
        private readonly DiagnosticTagProducer<DiagnosticsSuggestionTaggerProvider> _producer = new DiagnosticTagProducer<DiagnosticsSuggestionTaggerProvider>();

        [WpfFact, Trait(Traits.Feature, Traits.Features.SuggestionTags)]
        public async Task SuggestionTagTest1()
        {
            var (spans, selection) = await GetTagSpansAndSelectionAsync(
@"class C {
    void M() {
        var v = [|ne|]w X();
        v.Y = 1;
    }
}");
            Assert.Equal(1, spans.Length);
            Assert.Equal(selection, spans.Single().Span.Span.ToTextSpan());
        }

        private async Task<(ImmutableArray<ITagSpan<IErrorTag>> spans, TextSpan selection)> GetTagSpansAndSelectionAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(content);
            var analyzerMap = new Dictionary<string, DiagnosticAnalyzer[]>()
                {
                    { LanguageNames.CSharp, new DiagnosticAnalyzer[] { new CSharpUseObjectInitializerDiagnosticAnalyzer() } }
                };
            var spans = (await _producer.GetDiagnosticsAndErrorSpans(workspace, analyzerMap)).Item2;
            return (spans, workspace.Documents.Single().SelectedSpans.Single());
        }
    }
}
