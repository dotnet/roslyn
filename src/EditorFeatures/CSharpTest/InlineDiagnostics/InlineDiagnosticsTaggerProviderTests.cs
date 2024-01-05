﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
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
    [Trait(Traits.Feature, Traits.Features.ErrorSquiggles), Trait(Traits.Feature, Traits.Features.Tagging)]
    public class InlineDiagnosticsTaggerProviderTests
    {
        [WpfFact]
        public async Task ErrorTagGeneratedForError()
        {
            var spans = await GetTagSpansAsync("class C {");
            var firstSpan = Assert.Single(spans);
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        [WpfFact]
        public async Task ErrorTagGeneratedForErrorInSourceGeneratedDocument()
        {
            var spans = await GetTagSpansInSourceGeneratedDocumentAsync("class C {");
            var firstSpan = Assert.Single(spans);
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        private static async Task<ImmutableArray<ITagSpan<InlineDiagnosticsTag>>> GetTagSpansAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(content, composition: SquiggleUtilities.WpfCompositionWithSolutionCrawler);
            return await GetTagSpansAsync(workspace);
        }

        private static async Task<ImmutableArray<ITagSpan<InlineDiagnosticsTag>>> GetTagSpansInSourceGeneratedDocumentAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(
                files: Array.Empty<string>(),
                sourceGeneratedFiles: new[] { content },
                composition: SquiggleUtilities.WpfCompositionWithSolutionCrawler);
            return await GetTagSpansAsync(workspace);
        }

        private static async Task<ImmutableArray<ITagSpan<InlineDiagnosticsTag>>> GetTagSpansAsync(TestWorkspace workspace)
        {
            workspace.GlobalOptions.SetGlobalOption(InlineDiagnosticsOptionsStorage.EnableInlineDiagnostics, LanguageNames.CSharp, true);
            return (await TestDiagnosticTagProducer<InlineDiagnosticsTaggerProvider, InlineDiagnosticsTag>.GetDiagnosticsAndErrorSpans(workspace)).Item2;
        }
    }
}
