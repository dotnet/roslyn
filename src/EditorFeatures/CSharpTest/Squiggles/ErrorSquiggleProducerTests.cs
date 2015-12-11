// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Squiggles
{
    public class ErrorSquiggleProducerTests : AbstractSquiggleProducerTests
    {
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForError()
        {
            var spans = await GetErrorSpans("class C {");
            Assert.Equal(1, spans.Count());

            var firstSpan = spans.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorTagGeneratedForWarning()
        {
            var spans = await GetErrorSpans("class C { long x = 5l; }");
            Assert.Equal(1, spans.Count());
            Assert.Equal(PredefinedErrorTypeNames.Warning, spans.First().Tag.ErrorType);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
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

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml))
            {
                var spans = (await GetDiagnosticsAndErrorSpans(workspace)).Item2;

                Assert.Equal(1, spans.Count());
                Assert.Equal(PredefinedErrorTypeNames.SyntaxError, spans.First().Tag.ErrorType);
            }
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task SuggestionTagsForUnnecessaryCode()
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

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml))
            {
                var analyzerMap = new Dictionary<string, DiagnosticAnalyzer[]>
                {
                    {
                        LanguageNames.CSharp,
                        new DiagnosticAnalyzer[]
                        {
                            new CSharpSimplifyTypeNamesDiagnosticAnalyzer(),
                            new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer()
                        }
                    }
                };

                var spans =
                    (await GetDiagnosticsAndErrorSpans(workspace, analyzerMap)).Item2
                        .OrderBy(s => s.Span.Span.Start).ToImmutableArray();

                Assert.Equal(3, spans.Length);
                var first = spans[0];
                var second = spans[1];
                var third = spans[2];

                Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType);
                Assert.Equal(CSharpFeaturesResources.RemoveUnnecessaryUsingsDiagnosticTitle, first.Tag.ToolTipContent);
                Assert.Equal(40, first.Span.Start);
                Assert.Equal(25, first.Span.Length);

                Assert.Equal(PredefinedErrorTypeNames.Suggestion, second.Tag.ErrorType);
                Assert.Equal(CSharpFeaturesResources.RemoveUnnecessaryUsingsDiagnosticTitle, second.Tag.ToolTipContent);
                Assert.Equal(82, second.Span.Start);
                Assert.Equal(60, second.Span.Length);

                Assert.Equal(PredefinedErrorTypeNames.Suggestion, third.Tag.ErrorType);
                Assert.Equal(WorkspacesResources.NameCanBeSimplified, third.Tag.ToolTipContent);
                Assert.Equal(196, third.Span.Start);
                Assert.Equal(5, third.Span.Length);
            }
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task ErrorDoesNotCrashPastEOF()
        {
            var spans = await GetErrorSpans("class C { int x =");
            Assert.Equal(3, spans.Count());
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task SemanticErrorReported()
        {
            var spans = await GetErrorSpans("class C : Bar { }");
            Assert.Equal(1, spans.Count());

            var firstSpan = spans.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
            Assert.Contains("Bar", (string)firstSpan.Tag.ToolTipContent, StringComparison.Ordinal);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task TestNoErrorsAfterDocumentRemoved()
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync("class"))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
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
            }
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public async Task TestNoErrorsAfterProjectRemoved()
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync("class"))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
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
            }
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
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

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml))
            {
                var document = workspace.Documents.First();

                var updateArgs = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        new object(), workspace, workspace.CurrentSolution, document.Project.Id, document.Id,
                        ImmutableArray.Create(
                            CreateDiagnosticData(workspace, document, new TextSpan(0, 0)),
                            CreateDiagnosticData(workspace, document, new TextSpan(0, 1))));

                var spans = await GetErrorsFromUpdateSource(workspace, document, updateArgs);

                Assert.Equal(1, spans.Count());
                var first = spans.First();

                Assert.Equal(1, first.Span.Span.Length);
            }
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/6866"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
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

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml))
            {
                var document = workspace.Documents.First();

                var updateArgs = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        new LiveId(), workspace, workspace.CurrentSolution, document.Project.Id, document.Id,
                        ImmutableArray.Create(
                            CreateDiagnosticData(workspace, document, new TextSpan(0, 0)),
                            CreateDiagnosticData(workspace, document, new TextSpan(0, 1))));

                var spans = await GetErrorsFromUpdateSource(workspace, document, updateArgs);

                Assert.Equal(2, spans.Count());
                var first = spans.First();
                var second = spans.Last();

                Assert.Equal(1, first.Span.Span.Length);
                Assert.Equal(1, second.Span.Span.Length);
            }
        }

        private class LiveId : ISupportLiveUpdate
        {
            public LiveId()
            {
            }
        }

        private static async Task<IEnumerable<ITagSpan<IErrorTag>>> GetErrorSpans(params string[] content)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(content))
            {
                return (await GetDiagnosticsAndErrorSpans(workspace)).Item2;
            }
        }
    }
}
