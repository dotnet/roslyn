// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

[UseExportProvider]
public class ActiveStatementTrackingServiceTests
{
    [Theory, CombinatorialData]
    public async Task TrackingService_GetLatestSpansAsync(bool scheduleInitialTrackingBeforeOpenDoc)
    {
        var source1 = "class C { void F() => G(1); void G(int a) => System.Console.WriteLine(1); }";
        var source2 = "class D { }";
        var source3 = "/* GENERATE: class G { int X => 1; } */";

        var generator = new TestSourceGenerator() { ExecuteImpl = EditAndContinueWorkspaceTestBase.GenerateSource };

        var span11 = new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 15));
        var span12 = new LinePositionSpan(new LinePosition(0, 20), new LinePosition(0, 25));
        var span21 = new LinePositionSpan(new LinePosition(0, 11), new LinePosition(0, 16));
        var span22 = new LinePositionSpan(new LinePosition(0, 21), new LinePosition(0, 26));
        var spanG = new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 11));

        var spanProvider = new MockActiveStatementSpanProvider();

        spanProvider.GetBaseActiveStatementSpansImpl = (_, documentIds) => ImmutableArray.Create(
            ImmutableArray.Create(
                new ActiveStatementSpan(new ActiveStatementId(0), span11, ActiveStatementFlags.NonLeafFrame),
                new ActiveStatementSpan(new ActiveStatementId(1), span12, ActiveStatementFlags.LeafFrame),
                new ActiveStatementSpan(new ActiveStatementId(2), spanG, ActiveStatementFlags.LeafFrame)),
            ImmutableArray<ActiveStatementSpan>.Empty);

        spanProvider.GetAdjustedActiveStatementSpansImpl = (document, _) => document.Name switch
        {
            "1.cs" =>
            [
                new ActiveStatementSpan(new ActiveStatementId(0), span21, ActiveStatementFlags.NonLeafFrame),
                new ActiveStatementSpan(new ActiveStatementId(1), span22, ActiveStatementFlags.LeafFrame)
            ],
            "2.cs" => [],
            "3.g.cs" =>
            [
                new ActiveStatementSpan(new ActiveStatementId(2), spanG, ActiveStatementFlags.LeafFrame)
            ],
            _ => throw ExceptionUtilities.Unreachable()
        };

        //var testDocument1 = new EditorTestHostDocument(text: source1, displayName: "1.cs", exportProvider: workspace.ExportProvider, filePath: "1.cs");
        //var testDocument2 = new EditorTestHostDocument(text: source2, displayName: "2.cs", exportProvider: workspace.ExportProvider, filePath: "2.cs");
        //var testDocument3 = new EditorTestHostDocument(text: source2, displayName: "3.cs", exportProvider: workspace.ExportProvider, filePath: "2.cs");

        //var testDocumentGenerated = new EditorTestHostDocument(
        //    workspace.ExportProvider,
        //    workspace.Services.GetLanguageServices(LanguageNames.CSharp),
        //    code: source3,
        //    name: "3.g.cs",
        //    filePath: "3.g.cs",
        //    cursorPosition: null,
        //    spans: SpecializedCollections.EmptyDictionary<string, ImmutableArray<TextSpan>>(),
        //    generator: generator);

        //workspace.AddTestProject(new EditorTestHostProject(workspace, documents: [testDocument1, testDocument2, testDocument3, testDocumentGenerated]));

        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="1.cs"><![CDATA[
            class C { void F() => G(1); void G(int a) => System.Console.WriteLine(1); }
            ]]>
                    </Document>
                    <Document FilePath="2.cs"><![CDATA[
            class D { }
            ]]>
                    </Document>
                    <DocumentFromSourceGenerator><![CDATA[
            class G { int X => 1; }
            ]]>
                    </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """);

        var testDocument1 = workspace.Documents.Single(d => d.Name == "1.cs");
        var testDocument2 = workspace.Documents.Single(d => d.Name == "2.cs");
        var testDocumentGenerated = workspace.Documents.Single(d => d.IsSourceGenerated);

        // opens the documents
        var textBuffer1 = testDocument1.GetTextBuffer();
        var textBuffer2 = testDocument2.GetTextBuffer();
        var textBufferGenerated = testDocumentGenerated.GetTextBuffer();

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();
        var document1 = project.Documents.Single(d => d.Name == "1.cs");
        var document2 = project.Documents.Single(d => d.Name == "2.cs");
        var documentGenerated = (await project.GetSourceGeneratedDocumentsAsync()).Single(d => d is SourceGeneratedDocument);
        var snapshot1 = textBuffer1.CurrentSnapshot;
        var snapshot2 = textBuffer2.CurrentSnapshot;
        var snapshotGenerated = textBufferGenerated.CurrentSnapshot;
        Assert.Same(snapshot1, document1.GetTextSynchronously(CancellationToken.None).FindCorrespondingEditorTextSnapshot());
        Assert.Same(snapshot2, document2.GetTextSynchronously(CancellationToken.None).FindCorrespondingEditorTextSnapshot());
        Assert.Same(snapshotGenerated, documentGenerated.GetTextSynchronously(CancellationToken.None).FindCorrespondingEditorTextSnapshot());

        var trackingSession = new ActiveStatementTrackingService.TrackingSession(workspace, spanProvider);

        if (scheduleInitialTrackingBeforeOpenDoc)
        {
            await trackingSession.TrackActiveSpansAsync(solution);

            var spans1 = trackingSession.Test_GetTrackingSpans();
            AssertEx.Equal(
            [
                $"V0 →←@[10..15): NonLeafFrame",
                $"V0 →←@[20..25): LeafFrame"
            ], spans1[document1.FilePath].Select(s => $"{s.Span}: {s.Flags}"));

            var spans2 = await trackingSession.GetSpansAsync(solution, document1.Id, document1.FilePath, CancellationToken.None);
            AssertEx.Equal(["(0,10)-(0,15)", "(0,20)-(0,25)"], spans2.Select(s => s.LineSpan.ToString()));

            var spans3 = await trackingSession.GetSpansAsync(solution, document2.Id, document2.FilePath, CancellationToken.None);
            Assert.Empty(spans3);
        }

        var spans4 = await trackingSession.GetAdjustedTrackingSpansAsync(document1, snapshot1, CancellationToken.None);
        AssertEx.Equal(
        [
            $"V0 →←@[11..16): NonLeafFrame",
            $"V0 →←@[21..26): LeafFrame"
        ], spans4.Select(s => $"{s.Span}: {s.Flags}"));

        AssertEx.Empty(await trackingSession.GetAdjustedTrackingSpansAsync(document2, snapshot2, CancellationToken.None));

        if (!scheduleInitialTrackingBeforeOpenDoc)
        {
            await trackingSession.TrackActiveSpansAsync(solution);

            var spans5 = trackingSession.Test_GetTrackingSpans();
            AssertEx.Equal(
            [
                $"V0 →←@[11..16): NonLeafFrame",
                $"V0 →←@[21..26): LeafFrame"
            ], spans5[document1.FilePath].Select(s => $"{s.Span}: {s.Flags}"));
        }

        // we are not able to determine active statements in a document:
        spanProvider.GetAdjustedActiveStatementSpansImpl = (_, _) => ImmutableArray<ActiveStatementSpan>.Empty;

        var spans6 = await trackingSession.GetAdjustedTrackingSpansAsync(document1, snapshot1, CancellationToken.None);
        AssertEx.Equal(
        [
            $"V0 →←@[11..16): NonLeafFrame",
            $"V0 →←@[21..26): LeafFrame"
        ], spans6.Select(s => $"{s.Span}: {s.Flags}"));
    }
}
