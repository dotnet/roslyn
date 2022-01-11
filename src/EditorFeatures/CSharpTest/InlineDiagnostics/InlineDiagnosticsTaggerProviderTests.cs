// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDiagnostics
{
    [UseExportProvider]
    public class InlineDiagnosticsTaggerProviderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForError()
        {
            var spans = await GetTagSpansAsync("class C {");
            Assert.Equal(1, spans.Count());

            var firstSpan = spans.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        private static async Task<ImmutableArray<ITagSpan<InlineDiagnosticsTag>>> GetTagSpansAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(content, composition: SquiggleUtilities.WpfCompositionWithSolutionCrawler);
            return await GetTagSpansAsync(workspace);
        }

        private static async Task<ImmutableArray<ITagSpan<InlineDiagnosticsTag>>> GetTagSpansAsync(TestWorkspace workspace)
        {
            workspace.ApplyOptions(new[] { KeyValuePairUtil.Create(new OptionKey2(InlineDiagnosticsOptions.EnableInlineDiagnostics, LanguageNames.CSharp), (object)true) });
            return (await TestDiagnosticTagProducer<InlineDiagnosticsTaggerProvider, InlineDiagnosticsTag>.GetDiagnosticsAndErrorSpans(workspace)).Item2;
        }
    }
}
