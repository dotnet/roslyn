// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Squiggles
{
    [UseExportProvider]
    public class ErrorSquiggleProducerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForError()
        {
            var spans = await GetTagSpansAsync("class C {");
            var firstSpan = Assert.Single(spans);
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForErrorInSourceGeneratedDocument()
        {
            var spans = await GetTagSpansInSourceGeneratedDocumentAsync("class C {");
            var firstSpan = Assert.Single(spans);
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForWarning()
        {
            var spans = await GetTagSpansAsync("class C { long x = 5l; }");
            Assert.Equal(1, spans.Count());
            Assert.Equal(PredefinedErrorTypeNames.Warning, spans.First().Tag.ErrorType);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForWarningAsError()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <CompilationOptions ReportDiagnostic = ""Error"" />
            <Document FilePath = ""Test.cs"" >
                class Program
                {
                    void Test()
                    {
                        int a = 5;
                    }
                }
        </Document>
    </Project>
</Workspace>";

            using var workspace = TestWorkspace.Create(workspaceXml);
            var spans = (await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetDiagnosticsAndErrorSpans(workspace)).Item2;

            Assert.Equal(1, spans.Count());
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, spans.First().Tag.ErrorType);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task CustomizableTagsForUnnecessaryCode()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath = ""Test.cs"" >
// System is used - rest are unused.
using System.Collections;
using System;
using System.Diagnostics;
using System.Collections.Generic;

class Program
{
    void Test()
    {
        Int32 x = 2; // Int32 can be simplified.
        x += 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            using var workspace = TestWorkspace.Create(workspaceXml, composition: SquiggleUtilities.CompositionWithSolutionCrawler);
            var options = new Dictionary<OptionKey2, object>();
            var language = workspace.Projects.Single().Language;
            var preferIntrinsicPredefinedTypeOption = new OptionKey2(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language);
            var preferIntrinsicPredefinedTypeOptionValue = new CodeStyleOption2<bool>(value: true, notification: NotificationOption2.Error);
            options.Add(preferIntrinsicPredefinedTypeOption, preferIntrinsicPredefinedTypeOptionValue);

            workspace.ApplyOptions(options);

            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>
                {
                    {
                        LanguageNames.CSharp,
                        ImmutableArray.Create<DiagnosticAnalyzer>(
                            new CSharpSimplifyTypeNamesDiagnosticAnalyzer(),
                            new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer(),
                            new ReportOnClassWithLink())
                    }
                };

            var diagnosticsAndSpans = await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetDiagnosticsAndErrorSpans(workspace, analyzerMap);

            var spans =
                diagnosticsAndSpans.Item1
                    .Zip(diagnosticsAndSpans.Item2, (diagnostic, span) => (diagnostic, span))
                    .OrderBy(s => s.span.Span.Span.Start).ToImmutableArray();

            Assert.Equal(4, spans.Length);
            var first = spans[0].span;
            var second = spans[1].span;
            var third = spans[2].span;
            var fourth = spans[3].span;

            var expectedToolTip = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "IDE0005", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri("https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005", UriKind.Absolute)), "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005"),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                    new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, CSharpAnalyzersResources.Using_directive_is_unnecessary)));

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType);
            ToolTipAssert.EqualContent(expectedToolTip, first.Tag.ToolTipContent);
            Assert.Equal(40, first.Span.Start);
            Assert.Equal(25, first.Span.Length);

            expectedToolTip = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "IDE0005", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri("https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005", UriKind.Absolute)), "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005"),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                    new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, CSharpAnalyzersResources.Using_directive_is_unnecessary)));

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, second.Tag.ErrorType);
            ToolTipAssert.EqualContent(expectedToolTip, second.Tag.ToolTipContent);
            Assert.Equal(82, second.Span.Start);
            Assert.Equal(60, second.Span.Length);

            expectedToolTip = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "id", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri("https://github.com/dotnet/roslyn", UriKind.Absolute)), "https://github.com/dotnet/roslyn"),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                    new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "messageFormat")));

            Assert.Equal(PredefinedErrorTypeNames.Warning, third.Tag.ErrorType);
            ToolTipAssert.EqualContent(expectedToolTip, third.Tag.ToolTipContent);
            Assert.Equal(152, third.Span.Start);
            Assert.Equal(7, third.Span.Length);

            expectedToolTip = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "IDE0049", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri("https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0049", UriKind.Absolute)), "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0049"),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                    new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, WorkspacesResources.Name_can_be_simplified)));

            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, fourth.Tag.ErrorType);
            ToolTipAssert.EqualContent(expectedToolTip, fourth.Tag.ToolTipContent);
            Assert.Equal(196, fourth.Span.Start);
            Assert.Equal(5, fourth.Span.Length);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorDoesNotCrashPastEOF()
        {
            var spans = await GetTagSpansAsync("class C { int x =");
            Assert.Equal(3, spans.Count());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task SemanticErrorReported()
        {
            using var workspace = TestWorkspace.CreateCSharp("class C : Bar { }", composition: SquiggleUtilities.CompositionWithSolutionCrawler);

            var spans = await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetDiagnosticsAndErrorSpans(workspace);

            Assert.Equal(1, spans.Item2.Count());

            var firstDiagnostic = spans.Item1.First();
            var firstSpan = spans.Item2.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);

            var expectedToolTip = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(ClassificationTypeNames.Text, "CS0246", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0246)", UriKind.Absolute)), "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0246)"),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                    new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, firstDiagnostic.Message)));

            ToolTipAssert.EqualContent(expectedToolTip, firstSpan.Tag.ToolTipContent);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task TestNoErrorsAfterDocumentRemoved()
        {
            using var workspace = TestWorkspace.CreateCSharp("class");
            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace);
            var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
            using var disposable = tagger as IDisposable;
            await wrapper.WaitForTags();

            var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

            // Initially, while the buffer is associated with a Document, we should get
            // error squiggles.
            Assert.True(spans.Count > 0);

            // Now remove the document.
            workspace.CloseDocument(workspace.Documents.First().Id);
            workspace.OnDocumentRemoved(workspace.Documents.First().Id);
            await wrapper.WaitForTags();
            spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

            // And we should have no errors for this document.
            Assert.True(spans.Count == 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task TestNoErrorsAfterProjectRemoved()
        {
            using var workspace = TestWorkspace.CreateCSharp("class");
            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace);
            var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
            using var disposable = tagger as IDisposable;
            await wrapper.WaitForTags();

            var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

            // Initially, while the buffer is associated with a Document, we should get
            // error squiggles.
            Assert.True(spans.Count > 0);

            // Now remove the project.
            workspace.CloseDocument(workspace.Documents.First().Id);
            workspace.OnDocumentRemoved(workspace.Documents.First().Id);
            workspace.OnProjectRemoved(workspace.Projects.First().Id);
            await wrapper.WaitForTags();
            spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

            // And we should have no errors for this document.
            Assert.True(spans.Count == 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task BuildErrorZeroLengthSpan()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath = ""Test.cs"" >
            class Test
{
}
        </Document>
    </Project>
</Workspace>";

            using var workspace = TestWorkspace.Create(workspaceXml);
            var document = workspace.Documents.First();

            var updateArgs = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    new object(), workspace, workspace.CurrentSolution, document.Project.Id, document.Id,
                    ImmutableArray.Create(
                        TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.CreateDiagnosticData(document, new TextSpan(0, 0)),
                        TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.CreateDiagnosticData(document, new TextSpan(0, 1))));

            var spans = await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetErrorsFromUpdateSource(workspace, updateArgs);

            Assert.Equal(1, spans.Count());
            var first = spans.First();

            Assert.Equal(1, first.Span.Span.Length);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task LiveErrorZeroLengthSpan()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document FilePath = ""Test.cs"" >
            class Test
{
}
        </Document>
    </Project>
</Workspace>";

            using var workspace = TestWorkspace.Create(workspaceXml);
            var document = workspace.Documents.First();

            var updateArgs = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    new LiveId(), workspace, workspace.CurrentSolution, document.Project.Id, document.Id,
                    ImmutableArray.Create(
                        TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.CreateDiagnosticData(document, new TextSpan(0, 0)),
                        TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.CreateDiagnosticData(document, new TextSpan(0, 1))));

            var spans = await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetErrorsFromUpdateSource(workspace, updateArgs);

            Assert.Equal(2, spans.Count());
            var first = spans.First();
            var second = spans.Last();

            Assert.Equal(1, first.Span.Span.Length);
            Assert.Equal(1, second.Span.Span.Length);
        }

        private class LiveId : ISupportLiveUpdate
        {
            public LiveId()
            {
            }
        }

        private static async Task<ImmutableArray<ITagSpan<IErrorTag>>> GetTagSpansAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(content, composition: SquiggleUtilities.CompositionWithSolutionCrawler);
            return await GetTagSpansAsync(workspace);
        }

        private static async Task<ImmutableArray<ITagSpan<IErrorTag>>> GetTagSpansInSourceGeneratedDocumentAsync(string content)
        {
            using var workspace = TestWorkspace.CreateCSharp(
                files: Array.Empty<string>(),
                sourceGeneratedFiles: new[] { content },
                composition: SquiggleUtilities.WpfCompositionWithSolutionCrawler);
            return await GetTagSpansAsync(workspace);
        }

        private static async Task<ImmutableArray<ITagSpan<IErrorTag>>> GetTagSpansAsync(TestWorkspace workspace)
        {
            return (await TestDiagnosticTagProducer<DiagnosticsSquiggleTaggerProvider, IErrorTag>.GetDiagnosticsAndErrorSpans(workspace)).Item2;
        }

        private sealed class ReportOnClassWithLink : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                "id",
                "title",
                "messageFormat",
                "category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                "description",
                "https://github.com/dotnet/roslyn");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSymbolAction(
                    context =>
                    {
                        if (!context.Symbol.IsImplicitlyDeclared && context.Symbol.Locations.First().IsInSource)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First()));
                        }
                    },
                    SymbolKind.NamedType);
            }
        }
    }
}
