// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class ActiveStatementTrackingServiceTests : EditingTestBase
    {
        [Fact, WorkItem(846042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846042")]
        public void MovedOutsideOfMethod1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
    <AS:0>}</AS:0>

    static void Goo()
    {
        // tracking span moves to another method as the user types around it
        <TS:0>Goo(1);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfMethod2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Goo(1);</AS:0>
    }

    static void Goo()
    {
        <TS:0>Goo(2);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfLambda1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>}</AS:0>;
        <TS:0>Goo(1);</TS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MovedOutsideOfLambda2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
        Action b = () => { Goo(2); };
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Action a = () => { <AS:0>Goo(1);</AS:0> };
        Action b = () => { <TS:0>Goo(2);</TS:0> };
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Theory]
        [CombinatorialData]
        public async Task TrackingService_GetLatestSpansAsync(bool scheduleInitialTrackingBeforeOpenDoc)
        {
            var sourceV1 = "class C { void F() => G(1); void G(int a) => System.Console.WriteLine(1); }";

            using var workspace = new TestWorkspace();

            var span11 = new LinePositionSpan(new LinePosition(0, 10), new LinePosition(0, 15));
            var span12 = new LinePositionSpan(new LinePosition(0, 20), new LinePosition(0, 25));
            var span21 = new LinePositionSpan(new LinePosition(0, 11), new LinePosition(0, 16));
            var span22 = new LinePositionSpan(new LinePosition(0, 21), new LinePosition(0, 26));

            var encService = new MockEditAndContinueWorkspaceService();

            encService.GetBaseActiveStatementSpansAsyncImpl = documentIds => ImmutableArray.Create(ImmutableArray.Create(
                (span11, ActiveStatementFlags.IsNonLeafFrame),
                (span12, ActiveStatementFlags.IsLeafFrame)));

            encService.GetAdjustedDocumentActiveStatementSpansAsyncImpl = document => ImmutableArray.Create(
                (span21, ActiveStatementFlags.IsNonLeafFrame),
                (span22, ActiveStatementFlags.IsLeafFrame));

            var testDocument = new TestHostDocument(text: sourceV1, exportProvider: workspace.ExportProvider);
            workspace.AddTestProject(new TestHostProject(workspace, testDocument));
            var textBuffer = testDocument.GetTextBuffer();

            var solution = workspace.CurrentSolution;
            var project = solution.Projects.Single();
            var document = project.Documents.Single();
            var snapshot = textBuffer.CurrentSnapshot;
            Assert.Same(snapshot, document.GetTextSynchronously(CancellationToken.None).FindCorrespondingEditorTextSnapshot());

            var trackingSession = new ActiveStatementTrackingService.TrackingSession(workspace, encService);

            if (scheduleInitialTrackingBeforeOpenDoc)
            {
                await trackingSession.TrackActiveSpansAsync().ConfigureAwait(false);

                var spans1 = trackingSession.Test_GetTrackingSpans();
                AssertEx.Equal(new[]
                {
                    $"V0 →←@[10..15): IsNonLeafFrame",
                    $"V0 →←@[20..25): IsLeafFrame"
                }, spans1[document.Id].Select(s => $"{s.Span}: {s.Flags}"));
            }

            var spans2 = await trackingSession.GetAdjustedTrackingSpansAsync(document, snapshot, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"V0 →←@[11..16): IsNonLeafFrame",
                $"V0 →←@[21..26): IsLeafFrame"
            }, spans2.Select(s => $"{s.Span}: {s.Flags}"));

            if (!scheduleInitialTrackingBeforeOpenDoc)
            {
                await trackingSession.TrackActiveSpansAsync().ConfigureAwait(false);

                var spans1 = trackingSession.Test_GetTrackingSpans();
                AssertEx.Equal(new[]
                {
                    $"V0 →←@[11..16): IsNonLeafFrame",
                    $"V0 →←@[21..26): IsLeafFrame"
                }, spans1[document.Id].Select(s => $"{s.Span}: {s.Flags}"));
            }

            // we are not able to determine active statements in a document:
            encService.GetAdjustedDocumentActiveStatementSpansAsyncImpl = document => default;

            var spans3 = await trackingSession.GetAdjustedTrackingSpansAsync(document, snapshot, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"V0 →←@[11..16): IsNonLeafFrame",
                $"V0 →←@[21..26): IsLeafFrame"
            }, spans3.Select(s => $"{s.Span}: {s.Flags}"));
        }
    }
}
