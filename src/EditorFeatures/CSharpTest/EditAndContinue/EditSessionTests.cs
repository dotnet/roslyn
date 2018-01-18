// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class EditSessionTests : TestBase
    {
        internal sealed class TestActiveStatementProvider : IActiveStatementProvider
        {
            private readonly ImmutableArray<ActiveStatementDebugInfo> _infos;

            public TestActiveStatementProvider(ImmutableArray<ActiveStatementDebugInfo> infos) 
                => _infos = infos;

            public Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(_infos);
        }

        internal static ImmutableArray<ActiveStatementDebugInfo> GetActiveStatementDebugInfos(
            string[] markedSources,
            string extension = ".cs",
            int[] methodRowIds = null,
            Guid[] modules = null,
            int[] methodVersions = null,
            int[] ilOffsets = null,
            ActiveStatementFlags[] flags = null,
            ImmutableArray<Guid>[] threads = null)
        {
            IEnumerable<(TextSpan Span, int Id, SourceText Text, string DocumentName, DocumentId DocumentId)> EnumerateAllSpans()
            {
                int sourceIndex = 0;
                foreach (var markedSource in markedSources)
                {
                    var documentName = TestWorkspace.GetDefaultTestSourceDocumentName(sourceIndex, extension);
                    var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId(), documentName);
                    var text = SourceText.From(markedSource);

                    foreach (var (span, id) in ActiveStatementsDescription.GetActiveSpans(markedSource))
                    {
                        yield return (span, id, text, documentName, documentId);
                    }

                    sourceIndex++;
                }
            }

            IEnumerable<ActiveStatementDebugInfo> Enumerate()
            {
                var moduleId = new Guid("00000000-0000-0000-0000-000000000001");
                var threadId = new Guid("00000000-0000-0000-0000-000000000010");

                int index = 0;
                foreach (var (span, id, text, documentName, documentId) in EnumerateAllSpans().OrderBy(s => s.Id))
                {
                    yield return new ActiveStatementDebugInfo(
                        new ActiveInstructionId(
                            (modules != null) ? modules[index] : moduleId, 
                            methodToken: 0x06000000 | (methodRowIds != null ? methodRowIds[index] : index + 1),
                            methodVersion: (methodVersions != null) ? methodVersions[index] : 1,
                            ilOffset: (ilOffsets != null) ? ilOffsets[index] : 0),
                        documentName: documentName,
                        linePositionSpan: text.Lines.GetLinePositionSpan(span),
                        threadIds: (threads != null) ? threads[index] : ImmutableArray.Create(threadId),
                        flags: (flags != null) ? flags[index] : ((id == 0 ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame) | ActiveStatementFlags.MethodUpToDate));

                    index++;
                }
            }

            return Enumerate().ToImmutableArray();
        }

        internal static async Task<(ActiveStatementsMap, ImmutableArray<ActiveStatementExceptionRegions>, ImmutableArray<DocumentId>)> GetBaseActiveStatementsAndExceptionRegions(
            string[] markedSource,
            ImmutableArray<ActiveStatementDebugInfo> activeStatements,
            ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions = null,
            Func<Solution, Solution> adjustSolution = null)
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(typeof(CSharpEditAndContinueAnalyzer)));

            using (var workspace = TestWorkspace.CreateCSharp(
                ActiveStatementsDescription.ClearTags(markedSource),
                exportProvider: exportProvider))
            {
                var baseSolution = workspace.CurrentSolution;
                if (adjustSolution != null)
                {
                    baseSolution = adjustSolution(baseSolution);
                }

                var docsIds = from p in baseSolution.Projects
                              from d in p.DocumentIds
                              select d;

                var debuggingSession = new DebuggingSession(baseSolution);
                var activeStatementProvider = new TestActiveStatementProvider(activeStatements);

                var editSession = new EditSession(
                    baseSolution,
                    debuggingSession,
                    activeStatementProvider,
                    ImmutableDictionary<ProjectId, ProjectReadOnlyReason>.Empty,
                    nonRemappableRegions ?? ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
                    stoppedAtException: false);

                return (await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false),
                        await editSession.BaseActiveExceptionRegions.GetValueAsync(CancellationToken.None).ConfigureAwait(false),
                        docsIds.ToImmutableArray());
            }
        }

        private static string InspectAS(ActiveStatement statement)
            => $"{statement.Ordinal}: {statement.Span} flags=[{statement.Flags}] pdid={statement.PrimaryDocumentId.DebugName} docs=[{string.Join(",", statement.DocumentIds.Select(d => d.DebugName))}]";

        private static string InspectActiveStatementAndInstruction(ActiveStatement statement)
            => InspectAS(statement) + " " + statement.InstructionId.GetDebuggerDisplay();

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions()
        {
            var markedSource = new[]
            {
@"class Test1
{
    static void M1()
    {
        try { } finally { <AS:1>F1();</AS:1> }
    }

    static void F1()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}",
@"class Test2
{
    static void M2()
    {
        try
        {
          try
          {
              <AS:3>F2();</AS:3>
          }
          catch (Exception1 e1)
          {
          }
        }
        catch (Exception2 e2)
        {
        }
    }

    static void F2()
    {
        <AS:2>Test1.M1()</AS:2>
    }

    static void Main()
    {
        try { <AS:4>M2();</AS:4> } finally { }
    }
}
"
            };

            var module1 = new Guid("11111111-1111-1111-1111-111111111111");
            var module2 = new Guid("22222222-2222-2222-2222-222222222222");

            var activeStatements = GetActiveStatementDebugInfos(markedSource,
                methodRowIds: new[] { 1, 2, 3, 4, 5 },
                ilOffsets: new[] { 1, 1, 1, 2, 3 },
                modules: new[] { module1, module1, module2, module2, module2 });

            // add an extra active statement from non-Roslyn project, it should be ignored:
            activeStatements = activeStatements.Add(
                new ActiveStatementDebugInfo(
                    new ActiveInstructionId(moduleId: Guid.NewGuid(), methodToken: 0x06000005, methodVersion: 1, ilOffset: 10),
                    "NonRoslynDocument.mcpp",
                    new LinePositionSpan(new LinePosition(1, 1), new LinePosition(1, 10)),
                    threadIds: ImmutableArray.Create(Guid.NewGuid()),
                    ActiveStatementFlags.IsNonLeafFrame));

            var (baseActiveStatements, baseExceptionRegions, docs) = await GetBaseActiveStatementsAndExceptionRegions(markedSource, activeStatements).ConfigureAwait(false);

            // Active Statements

            var statements = baseActiveStatements.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();
            AssertEx.Equal(new[]
            {
                "0: (9,14)-(9,35) flags=[IsLeafFrame, MethodUpToDate] pdid=test1.cs docs=[test1.cs] mvid=11111111-1111-1111-1111-111111111111 0x06000001 v1 IL_0001",
                "1: (4,32)-(4,37) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs] mvid=11111111-1111-1111-1111-111111111111 0x06000002 v1 IL_0001",
                "2: (21,14)-(21,24) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs] mvid=22222222-2222-2222-2222-222222222222 0x06000003 v1 IL_0001",
                "3: (8,20)-(8,25) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs] mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 IL_0002",
                "4: (26,20)-(26,25) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs] mvid=22222222-2222-2222-2222-222222222222 0x06000005 v1 IL_0003"
            }, statements.Select(InspectActiveStatementAndInstruction));

            // Active Statements per document

            Assert.Equal(2, baseActiveStatements.DocumentMap.Count);

            AssertEx.Equal(new[]
            {
                "0: (9,14)-(9,35) flags=[IsLeafFrame, MethodUpToDate] pdid=test1.cs docs=[test1.cs]",
                "1: (4,32)-(4,37) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs]"
            }, baseActiveStatements.DocumentMap[docs[0]].Select(InspectAS));

            AssertEx.Equal(new[]
            {
                "2: (21,14)-(21,24) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs]",
                "3: (8,20)-(8,25) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs]",
                "4: (26,20)-(26,25) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test2.cs docs=[test2.cs]"
            }, baseActiveStatements.DocumentMap[docs[1]].Select(InspectAS));

           // Exception Regions

            AssertEx.Equal(new[]
            {
                "[]",
                "[(4,8)-(4,46)]",
                "[]",
                "[(14,8)-(16,9),(10,10)-(12,11)]",
                "[(26,35)-(26,46)]"
            }, baseExceptionRegions.Select(r => "[" + string.Join(",", r.Spans) + "]"));

            // GetActiveStatementAndExceptionRegionSpans

            // Assume 2 updates in Project2:
            //   Test2.M2: adding a line in front of try-catch.
            //   Test2.F2: moving the entire method 2 lines down.

            LinePositionSpan AddDelta(LinePositionSpan span, int lineDelta)
                => new LinePositionSpan(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

            var newActiveStatementsInChangedDocuments = ImmutableArray.Create(
                (
                    docs[1], 

                    ImmutableArray.Create(
                        statements[2].WithSpan(AddDelta(statements[2].Span, +2)),
                        statements[3].WithSpan(AddDelta(statements[3].Span, +1)),
                        statements[4]),

                    ImmutableArray.Create(
                        baseExceptionRegions[2].Spans,
                        baseExceptionRegions[3].Spans.SelectAsArray(es => AddDelta(es, +1)),
                        baseExceptionRegions[4].Spans)
                )
            );

            EditSession.GetActiveStatementAndExceptionRegionSpans(
                module2,
                baseActiveStatements,
                baseExceptionRegions,
                updatedMethodTokens: new[] { 0x06000004 }, // contains only recompiled methods in the project we are interested in (module2)
                ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
                newActiveStatementsInChangedDocuments,
                out var activeStatementsInUpdatedMethods,
                out var nonRemappableRegions);
            
            AssertEx.Equal(new[]
            {
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | AS (8,20)-(8,25) δ=1",
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | ER (14,8)-(16,9) δ=1",
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | ER (10,10)-(12,11) δ=1"
            }, nonRemappableRegions.Select(r => $"{r.Method.GetDebuggerDisplay()} | {r.Region.GetDebuggerDisplay()}"));

            AssertEx.Equal(new[]
            {
                "thread=00000000-0000-0000-0000-000000000010 mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 IL_0002: (9,20)-(9,25)"
            }, activeStatementsInUpdatedMethods.Select(v => $"thread={v.ThreadId} {v.OldInstructionId.GetDebuggerDisplay()}: {v.NewSpan}"));
        }

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions_WithInitialNonRemappableRegions()
        {
            var markedSource =
@"class Test
{
    static void F1()
    {
        try
        {
            <AS:0>M();</AS:0>
        }
        <ER:0.0>catch
        {
        }</ER:0.0>
    }

    static void F2()
    {
        try
        {
        }
        <ER:1.0>catch
        {
            <AS:1>M();</AS:1>
        }</ER:1.0>
    }

    static void F3()
    {   
        try
        {
            try 
            {
                <AS:2>M();</AS:2>
            }
            <ER:2.0>finally
            {
            }</ER:2.0>
        }
        <ER:2.1>catch
        {
        }</ER:2.1>
    }

    static void F4()
    {
        try
        {
            try
            {
            }
            <ER:3.0>catch
            {
                <AS:3>M();</AS:3>
            }</ER:3.0>
        }
        <ER:3.1>catch
        {
        }}</ER:3.1>
    }
}";

            var module1 = new Guid("11111111-1111-1111-1111-111111111111");
            var sourceText = SourceText.From(markedSource);

            var activeStatements = GetActiveStatementDebugInfos(new[] { markedSource },
                modules: new[] { module1, module1, module1, module1 },
                methodVersions: new[] { 2, 2, 1, 1 }, // method F3 and F4 were not remapped
                flags: new[] 
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, // F1
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, // F2
                    ActiveStatementFlags.None | ActiveStatementFlags.IsNonLeafFrame,           // F3
                    ActiveStatementFlags.None | ActiveStatementFlags.IsNonLeafFrame,           // F4
                });

            var exceptionSpans = ActiveStatementsDescription.GetExceptionRegions(markedSource, activeStatements.Length);

            var span2 = activeStatements[2].LinePositionSpan;
            var er20 = sourceText.Lines.GetLinePositionSpan(exceptionSpans[2][0]);
            var er21 = sourceText.Lines.GetLinePositionSpan(exceptionSpans[2][1]);
            var span3 = activeStatements[3].LinePositionSpan;
            var er30 = sourceText.Lines.GetLinePositionSpan(exceptionSpans[3][0]);
            var er31 = sourceText.Lines.GetLinePositionSpan(exceptionSpans[3][1]);

            // assume that the follwing edits have been made:
            var initialNonRemappableRegions = new Dictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>
            {
                { new ActiveMethodId(module1, 0x06000003, 1), ImmutableArray.Create(
                    new NonRemappableRegion(span2, lineDelta: -1, isExceptionRegion: false),
                    new NonRemappableRegion(er20, lineDelta: +2, isExceptionRegion: false),
                    new NonRemappableRegion(er21, lineDelta: +2, isExceptionRegion: false)) },
                { new ActiveMethodId(module1, 0x06000004, 1), ImmutableArray.Create(
                    new NonRemappableRegion(span3, lineDelta: +1, isExceptionRegion: false),
                    new NonRemappableRegion(er30, lineDelta: +1, isExceptionRegion: false),
                    new NonRemappableRegion(er31, lineDelta: +1, isExceptionRegion: false)) }
            };

            var (baseActiveStatements, baseExceptionRegions, docs) = await GetBaseActiveStatementsAndExceptionRegions(new[] { markedSource }, activeStatements).ConfigureAwait(false);

            // Active Statements
#if TODO
            var statements = baseActiveStatements.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();
            AssertEx.Equal(new[]
            {
                "",
            }, statements.Select(InspectActiveStatementAndInstruction));

            // Exception Regions

            AssertEx.Equal(new[]
            {
                "[]",
            }, baseExceptionRegions.Select(r => "[" + string.Join(",", r.Spans) + "]"));

            // GetActiveStatementAndExceptionRegionSpans

            // Assume 2 updates:

            var newActiveStatementsInChangedDocuments = ImmutableArray.Create(
                (
                    docs[1],

                    ImmutableArray.Create(
                        statements[2].WithSpan(statements[2].Span.AddLineDelta(+2)),
                        statements[3].WithSpan(statements[3].Span.AddLineDelta(+1)),
                        statements[4]),

                    ImmutableArray.Create(
                        baseExceptionRegions[2].Spans,
                        baseExceptionRegions[3].Spans.SelectAsArray(es => es.AddLineDelta(+1)),
                        baseExceptionRegions[4].Spans)
                )
            );

            EditSession.GetActiveStatementAndExceptionRegionSpans(
                module1,
                baseActiveStatements,
                baseExceptionRegions,
                updatedMethodTokens: new[] { 0x06000001 }, // contains only recompiled methods in the project we are interested in (module2)
                ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
                newActiveStatementsInChangedDocuments,
                out var activeStatementsInUpdatedMethods,
                out var nonRemappableRegions);

            AssertEx.Equal(new[]
            {
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | AS (8,20)-(8,25) δ=1",
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | ER (14,8)-(16,9) δ=1",
                "mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 | ER (10,10)-(12,11) δ=1"
            }, nonRemappableRegions.Select(r => $"{r.Method.GetDebuggerDisplay()} | {r.Region.GetDebuggerDisplay()}"));

            AssertEx.Equal(new[]
            {
                "thread=00000000-0000-0000-0000-000000000010 mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 IL_0002: (9,20)-(9,25)"
            }, activeStatementsInUpdatedMethods.Select(v => $"thread={v.ThreadId} {v.OldInstructionId.GetDebuggerDisplay()}: {v.NewSpan}"));

            // GetActiveStatementAndExceptionRegionSpans with initial remaps

#endif
        }

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions_Recursion()
        {
            var markedSource = new[]
            {
@"class C
{
    static void M()
    {
        try 
        {
            <AS:1>M();</AS:1>
        }
        catch (Exception e)
        {
        }
    }

    static void F()
    {
        <AS:0>M();</AS:0>
    }
}"
            };

            var thread1 = Guid.NewGuid();
            var thread2 = Guid.NewGuid();

            // Thread1 stack trace: F (AS:0), M (AS:1 leaf)
            // Thread2 stack trace: F (AS:0), M (AS:1), M (AS:1 leaf)

            var activeStatements = GetActiveStatementDebugInfos(
                markedSource,
                methodRowIds: new[] { 1, 2 },
                ilOffsets: new[] { 1, 1 },
                flags: new[] 
                {
                    ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.NonUserCode | ActiveStatementFlags.PartiallyExecuted | ActiveStatementFlags.MethodUpToDate,
                    ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.IsLeafFrame | ActiveStatementFlags.MethodUpToDate
                },
                threads: new[] { ImmutableArray.Create(thread1, thread2), ImmutableArray.Create(thread1, thread2, thread2) });

            var (baseActiveStatements, baseExceptionRegions, docs) = await GetBaseActiveStatementsAndExceptionRegions(markedSource, activeStatements).ConfigureAwait(false);

            // Active Statements

            Assert.Equal(1, baseActiveStatements.DocumentMap.Count);

            AssertEx.Equal(new[] 
            {
                "0: (15,14)-(15,18) flags=[PartiallyExecuted, NonUserCode, MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs]",
                "1: (6,18)-(6,22) flags=[IsLeafFrame, MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs]",
            }, baseActiveStatements.DocumentMap[docs[0]].Select(InspectAS));

            Assert.Equal(2, baseActiveStatements.InstructionMap.Count);

            var statements = baseActiveStatements.InstructionMap.Values.OrderBy(v => v.InstructionId.MethodToken).ToArray();
            var s = statements[0];
            Assert.Equal(0x06000001, s.InstructionId.MethodToken);
            Assert.Equal(0, s.PrimaryDocumentOrdinal);
            Assert.Equal(docs[0], s.DocumentIds.Single());
            Assert.True(s.IsNonLeaf);
            AssertEx.Equal(new[] { thread1, thread2 }, s.ThreadIds);

            s = statements[1];
            Assert.Equal(0x06000002, s.InstructionId.MethodToken);
            Assert.Equal(1, s.PrimaryDocumentOrdinal);
            Assert.Equal(docs[0], s.DocumentIds.Single());
            Assert.True(s.IsLeaf);
            Assert.True(s.IsNonLeaf);
            AssertEx.Equal(new[] { thread1, thread2, thread2 }, s.ThreadIds);

            // Exception Regions

            AssertEx.Equal(new[]
            {
                "[]",
                "[(8,8)-(10,9)]"
            }, baseExceptionRegions.Select(r => "[" + string.Join(",", r.Spans) + "]"));
        }

        [Fact, WorkItem(24320, "https://github.com/dotnet/roslyn/issues/24320")]
        public async Task BaseActiveStatementsAndExceptionRegions_LinkedDocuments()
        {
            var markedSource = new[]
            {
@"class Test1
{
    static void Main() => <AS:2>Project2::Test1.F();</AS:2>
    static void F() => <AS:1>Project4::Test2.M();</AS:1>
}",
@"
class Test2
{
    static void M() => <AS:0>Console.WriteLine();</AS:0>
}"
            };

            var module1 = Guid.NewGuid();
            var module2 = Guid.NewGuid();
            var module4 = Guid.NewGuid();

            var activeStatements = GetActiveStatementDebugInfos(
                markedSource,
                methodRowIds: new[] { 1, 2, 1 },
                modules: new[] { module4, module2, module1 });

            // Project1: Test1.cs [AS 2], Test2.cs      
            // Project2: Test1.cs (link from P1) [AS 1]
            // Project3: Test1.cs (link from P1)
            // Project4: Test2.cs (link from P1) [AS 0]

            var adjustSolution = new Func<Solution, Solution>(solution =>
            {
                var project1 = solution.Projects.Single();
                var doc1 = project1.Documents.First();
                var doc2 = project1.Documents.Skip(1).First();
                Assert.True(doc1.TryGetText(out var text1));
                Assert.True(doc2.TryGetText(out var text2));

                void AddProjectAndLinkDocument(string projectName, Document doc, SourceText text)
                {
                    var p = solution.AddProject(projectName, projectName, "C#");
                    solution = p.Solution.AddDocument(DocumentId.CreateNewId(p.Id, projectName + "->" + doc.Name), doc.Name, text, filePath: doc.FilePath);
                }

                AddProjectAndLinkDocument("Project2", doc1, text1);
                AddProjectAndLinkDocument("Project3", doc1, text1);
                AddProjectAndLinkDocument("Project4", doc2, text2);
                return solution;
            });

            var (baseActiveStatements, baseExceptionRegions, docs) = await GetBaseActiveStatementsAndExceptionRegions(markedSource, activeStatements, adjustSolution: adjustSolution).ConfigureAwait(false);

            // Active Statements

            var documentMap = baseActiveStatements.DocumentMap;

            Assert.Equal(5, docs.Length);
            Assert.Equal(5, documentMap.Count);

            // TODO: currently we associate all linked documents to the AS regardless of whether they belong to a project that matches the AS module.
            // https://github.com/dotnet/roslyn/issues/24320

            AssertEx.Equal(new[]
            {
                "1: (3,29)-(3,49) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]",
                "2: (2,32)-(2,52) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]"
            }, documentMap[docs[0]].Select(InspectAS));

            AssertEx.Equal(new[]
            {
                "0: (3,29)-(3,49) flags=[IsLeafFrame, MethodUpToDate] pdid=test2.cs docs=[test2.cs,Project4->test2.cs]",
            }, documentMap[docs[1]].Select(InspectAS));

            AssertEx.Equal(new[]
            {
                "1: (3,29)-(3,49) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]",
                "2: (2,32)-(2,52) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]"
            }, documentMap[docs[2]].Select(InspectAS));

            AssertEx.Equal(new[]
            {
                "1: (3,29)-(3,49) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]",
                "2: (2,32)-(2,52) flags=[MethodUpToDate, IsNonLeafFrame] pdid=test1.cs docs=[test1.cs,Project2->test1.cs,Project3->test1.cs]"
            }, documentMap[docs[3]].Select(InspectAS));

            AssertEx.Equal(new[]
            {
                "0: (3,29)-(3,49) flags=[IsLeafFrame, MethodUpToDate] pdid=test2.cs docs=[test2.cs,Project4->test2.cs]",
            }, documentMap[docs[4]].Select(InspectAS));

            Assert.Equal(3, baseActiveStatements.InstructionMap.Count);

            var statements = baseActiveStatements.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();
            var s = statements[0];
            Assert.Equal(0x06000001, s.InstructionId.MethodToken);
            Assert.Equal(module4, s.InstructionId.ModuleId);

            s = statements[1];
            Assert.Equal(0x06000002, s.InstructionId.MethodToken);
            Assert.Equal(module2, s.InstructionId.ModuleId);

            s = statements[2];
            Assert.Equal(0x06000001, s.InstructionId.MethodToken);
            Assert.Equal(module1, s.InstructionId.ModuleId);
        }
    }
}
