// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
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

        private static async Task<(ImmutableArray<ITagSpan<IErrorTag>> spans, TextSpan selection)> GetTagSpansAndSelectionAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(content);
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>()
            {
                { LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpUseObjectInitializerDiagnosticAnalyzer()) }
            };

            var spans = (await TestDiagnosticTagProducer<DiagnosticsSuggestionTaggerProvider, IErrorTag>.GetDiagnosticsAndErrorSpans(workspace, analyzerMap)).Item2;
            return (spans, workspace.Documents.Single().SelectedSpans.Single());
        }
    }
}
