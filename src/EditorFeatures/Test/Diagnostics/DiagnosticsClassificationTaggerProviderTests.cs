// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
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
    [Trait(Traits.Feature, Traits.Features.Diagnostics), Trait(Traits.Feature, Traits.Features.Tagging)]
    public class DiagnosticsClassificationTaggerProviderTests
    {
        [WpfTheory, CombinatorialData]
        public async Task Test_FadingSpans(bool throughAdditionalLocations)
        {
            var analyzer = new Analyzer(diagnosticId: "test", throughAdditionalLocations);
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>
            {
                {  LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(analyzer) }
            };

            using var workspace = EditorTestWorkspace.CreateCSharp(new string[] { "class A { }", "class E { }" }, parseOptions: CSharpParseOptions.Default, composition: SquiggleUtilities.CompositionWithSolutionCrawler);
            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsClassificationTaggerProvider, ClassificationTag>(workspace, analyzerMap);

            var firstDocument = workspace.Documents.First();
            var tagger = wrapper.TaggerProvider.CreateTagger<ClassificationTag>(firstDocument.GetTextBuffer());
            using var disposable = tagger as IDisposable;
            // test first update
            await wrapper.WaitForTags();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            if (!throughAdditionalLocations)
            {
                // We should get a single tag span, which is diagnostic's primary location.
                Assert.Equal(1, spans.Count);

                Assert.Equal(new Span(0, 10), spans[0].Span.Span);

                Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[0].Tag.ClassificationType.Classification);
            }
            else
            {
                // We should get two spans, the 1-index and 2-index additional locations in the original diagnostic.
                Assert.Equal(2, spans.Count);

                Assert.Equal(new Span(0, 1), spans[0].Span.Span);
                Assert.Equal(new Span(9, 1), spans[1].Span.Span);

                Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[0].Tag.ClassificationType.Classification);
                Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[1].Tag.ClassificationType.Classification);
            }
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            private readonly bool _throughAdditionalLocations;
            private readonly DiagnosticDescriptor _rule;

            public Analyzer(string diagnosticId, bool throughAdditionalLocations)
            {
                _throughAdditionalLocations = throughAdditionalLocations;
                _rule = new(
                    diagnosticId, "test", "test", "test", DiagnosticSeverity.Error, true,
                    customTags: DiagnosticCustomTags.Create(isUnnecessary: true, isConfigurable: false, isCustomConfigurable: false, EnforceOnBuild.Never));
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                => ImmutableArray.Create(_rule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    var primaryLocation = Location.Create(c.Tree, new TextSpan(0, 10));
                    if (!_throughAdditionalLocations)
                    {
                        c.ReportDiagnostic(DiagnosticHelper.Create(
                            _rule, primaryLocation,
                            NotificationOption2.Error,
                            additionalLocations: null,
                            properties: null));
                    }
                    else
                    {
                        var additionalLocations = ImmutableArray.Create(Location.Create(c.Tree, new TextSpan(0, 10)));
                        var additionalUnnecessaryLocations = ImmutableArray.Create(
                            Location.Create(c.Tree, new TextSpan(0, 1)),
                            Location.Create(c.Tree, new TextSpan(9, 1)));

                        c.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                            _rule, primaryLocation,
                            NotificationOption2.Error,
                            additionalLocations,
                            additionalUnnecessaryLocations));
                    }
                });
            }
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/62183")]
        [InlineData(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId, true)]
        [InlineData(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId, false)]
        [InlineData(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId, true)]
        [InlineData(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId, false)]
        public async Task Test_FadingOptions(string diagnosticId, bool fadingOptionValue)
        {
            var analyzer = new Analyzer(diagnosticId, throughAdditionalLocations: false);
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>
            {
                {  LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(analyzer) }
            };

            using var workspace = EditorTestWorkspace.CreateCSharp(new string[] { "class A { }", "class E { }" }, parseOptions: CSharpParseOptions.Default, composition: SquiggleUtilities.CompositionWithSolutionCrawler);

            // Set fading option
            var fadingOption = GetFadingOptionForDiagnostic(diagnosticId);
            workspace.GlobalOptions.SetGlobalOption(fadingOption, LanguageNames.CSharp, fadingOptionValue);

            // Add mapping from diagnostic ID to fading option
            IDEDiagnosticIdToOptionMappingHelper.AddFadingOptionMapping(diagnosticId, fadingOption);

            // Set up the tagger
            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsClassificationTaggerProvider, ClassificationTag>(workspace, analyzerMap);

            var firstDocument = workspace.Documents.First();
            var tagger = wrapper.TaggerProvider.CreateTagger<ClassificationTag>(firstDocument.GetTextBuffer());
            using var disposable = tagger as IDisposable;
            // test first update
            await wrapper.WaitForTags();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            if (!fadingOptionValue)
            {
                // We should get no tag spans when the fading option is disabled.
                Assert.Empty(spans);
            }
            else
            {
                // We should get a single tag span, which is diagnostic's primary location.
                Assert.Equal(1, spans.Count);

                Assert.Equal(new Span(0, 10), spans[0].Span.Span);

                Assert.Equal(ClassificationTypeDefinitions.UnnecessaryCode, spans[0].Tag.ClassificationType.Classification);
            }
        }

        private static PerLanguageOption2<bool> GetFadingOptionForDiagnostic(string diagnosticId)
            => diagnosticId switch
            {
                IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId => FadingOptions.FadeOutUnusedImports,
                IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId => FadingOptions.FadeOutUnreachableCode,
                _ => throw ExceptionUtilities.UnexpectedValue(diagnosticId),
            };
    }
}
