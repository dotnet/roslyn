// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class ActiveStatementsMapTests
    {
        [Theory]
        [InlineData(/*span*/ 3, 0, 5, 2,     /*expected*/ 0, 4)]
        [InlineData(/*span*/ 2, 0, 3, 1,     /*expected*/ 0, 1)]
        [InlineData(/*span*/ 19, 1, 19, 100, /*expected*/ 0, 0)]
        [InlineData(/*span*/ 20, 1, 20, 2,   /*expected*/ 0, 0)]
        [InlineData(/*span*/ 0, 0, 100, 0,   /*expected*/ 0, 6)]
        public void GetSpansStartingInSpan1(int sl, int sc, int el, int ec, int s, int e)
        {
            var span = new LinePositionSpan(new(sl, sc), new(el, ec));
            var array = ImmutableArray.Create(
                new LinePositionSpan(new(3, 0), new(3, 1)),
                new LinePositionSpan(new(3, 5), new(3, 6)),
                new LinePositionSpan(new(4, 4), new(4, 18)),
                new LinePositionSpan(new(5, 1), new(5, 2)),
                new LinePositionSpan(new(5, 2), new(5, 8)),
                new LinePositionSpan(new(19, 0), new(19, 42)));

            Assert.Equal(new Range(s, e), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
        }

        [Fact]
        public void GetSpansStartingInSpan2()
        {
            var span = TextSpan.FromBounds(8, 11);

            var array = ImmutableArray.Create(
                TextSpan.FromBounds(1, 6), // does not overlap
                TextSpan.FromBounds(3, 9), // overlaps
                TextSpan.FromBounds(4, 5), // does not overlap
                TextSpan.FromBounds(6, 7), // does not overlap
                TextSpan.FromBounds(7, 9), // overlaps
                TextSpan.FromBounds(10, 12), // overlaps
                TextSpan.FromBounds(13, 15)); // does not overlap

            // only one span has start position within the span:
            Assert.Equal(new Range(5, 6), ActiveStatementsMap.GetSpansStartingInSpan(span.Start, span.End, array, startPositionComparer: (x, y) => x.Start.CompareTo(y)));
        }

        [Fact]
        public async Task Ordering()
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

            var source = @"
class C
{
    void F()
    {
#line 2 ""x""
S1();
S2();
S3();
#line 1 ""x""    
S0();
S1();
S2();
#line 5 ""x""
S4();
S5();
S5();
#line default
    }
}";

            var solution = workspace.CurrentSolution
                .AddProject("proj", "proj", LanguageNames.CSharp)
                .AddDocument("doc", SourceText.From(source, Encoding.UTF8), filePath: "a.cs").Project.Solution;

            var project = solution.Projects.Single();
            var document = project.Documents.Single();
            var analyzer = project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

            var documentPathMap = new Dictionary<string, ImmutableArray<ActiveStatement>>();

            var moduleId = Guid.NewGuid();
            var token = 0x06000001;
            ManagedActiveStatementDebugInfo CreateInfo(int startLine, int startColumn, int endLine, int endColumn, string fileName)
                => new(new(new(moduleId, token++, version: 1), ilOffset: 0), fileName, new SourceSpan(startLine, startColumn, endLine, endColumn), ActiveStatementFlags.None);

            var debugInfos = ImmutableArray.Create(
                CreateInfo(3, 0, 3, 4, "x"),
                CreateInfo(6, 0, 6, 4, "x"),
                CreateInfo(4, 0, 4, 4, "x"),
                CreateInfo(2, 0, 2, 4, "x"),
                CreateInfo(5, 0, 5, 4, "x"),
                CreateInfo(0, 0, 0, 4, "x"),
                CreateInfo(1, 0, 1, 4, "x")
            );

            var map = ActiveStatementsMap.Create(debugInfos, remapping: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty);

            var oldSpans = await map.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None);

            AssertEx.Equal(new[]
            {
                "[48..52) -> (1,0)-(1,4) #6",
                "[55..59) -> (2,0)-(2,4) #3",
                "[62..66) -> (3,0)-(3,4) #0",
                "[86..90) -> (0,0)-(0,4) #5",
                "[120..124) -> (4,0)-(4,4) #2",
                "[127..131) -> (5,0)-(5,4) #4",
                "[134..138) -> (6,0)-(6,4) #1"
            }, oldSpans.Select(s => $"{s.UnmappedSpan} -> {s.Statement.Span} #{s.Statement.Ordinal}"));
        }
    }
}
