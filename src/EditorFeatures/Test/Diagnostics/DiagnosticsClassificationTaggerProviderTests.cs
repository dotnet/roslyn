﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    public class DiagnosticsClassificationTaggerProviderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task Test_FadingSpans()
        {
            var analyzer = new Analyzer();
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>
            {
                {  LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(analyzer) }
            };

            using var workspace = TestWorkspace.CreateCSharp(new string[] { "class A { }", "class E { }" }, CSharpParseOptions.Default);
            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsClassificationTaggerProvider, ClassificationTag>(workspace, analyzerMap);
            var tagger = wrapper.TaggerProvider.CreateTagger<ClassificationTag>(workspace.Documents.First().GetTextBuffer());
            using var disposable = tagger as IDisposable;
            // test first update
            await wrapper.WaitForTags();

            // We should get two spans, the 1-index and 2-index locations in the original diagnostic.
            var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            Assert.Equal(2, spans.Count);

            Assert.Equal(new Span(0, 1), spans[0].Span.Span);
            Assert.Equal(new Span(9, 1), spans[1].Span.Span);

            Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[0].Tag.ClassificationType.Classification);
            Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[1].Tag.ClassificationType.Classification);
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            // Mark elements 1, and 2 in the locations as the fading locations.
            private static readonly ImmutableDictionary<string, IEnumerable<int>> s_fadeLocations = new Dictionary<string, IEnumerable<int>>
            {
                { nameof(WellKnownDiagnosticTags.Unnecessary), new int[] { 1, 2 } },
            }.ToImmutableDictionary();

            private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
                "test", "test", "test", "test", DiagnosticSeverity.Error, true,
                customTags: DiagnosticCustomTags.Create(isUnnecessary: true, isConfigurable: false));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                => ImmutableArray.Create(_rule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    c.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        _rule, Location.Create(c.Tree, new TextSpan(0, 10)),
                        ReportDiagnostic.Error,
                        new Location[] {
                            Location.Create(c.Tree, new TextSpan(0, 10)),
                            Location.Create(c.Tree, new TextSpan(0, 1)),
                            Location.Create(c.Tree, new TextSpan(9, 1)),
                        },
                        s_fadeLocations));
                });
            }
        }
    }
}
