// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using System.Text;
using System.IO;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    using static ActiveStatementTestHelpers;

    [UseExportProvider]
    public class EditSessionActiveStatementsTests : TestBase
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(NoCompilationLanguageService));

        private static EditSession CreateEditSession(
            Solution solution,
            ImmutableArray<ManagedActiveStatementDebugInfo> activeStatements,
            ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions = null,
            CommittedSolution.DocumentState initialState = CommittedSolution.DocumentState.MatchesBuildOutput)
        {
            var mockDebuggerService = new MockManagedEditAndContinueDebuggerService()
            {
                GetActiveStatementsImpl = () => activeStatements,
            };

            var mockCompilationOutputsProvider = new Func<Project, CompilationOutputs>(_ => new MockCompilationOutputs(Guid.NewGuid()));

            var debuggingSession = new DebuggingSession(
                new DebuggingSessionId(1),
                solution,
                mockDebuggerService,
                mockCompilationOutputsProvider,
                NullPdbMatchingSourceTextProvider.Instance,
                SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>(),
                reportDiagnostics: true);

            if (initialState != CommittedSolution.DocumentState.None)
            {
                EditAndContinueWorkspaceServiceTests.SetDocumentsState(debuggingSession, solution, initialState);
            }

            debuggingSession.RestartEditSession(nonRemappableRegions ?? ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty, inBreakState: true, out _);
            return debuggingSession.EditSession;
        }

        private static async Task<Solution> AddDefaultTestSolutionAsync(TestWorkspace workspace, string[] markedSources)
        {
            var solution = workspace.CurrentSolution;

            var project = solution
                .AddProject("proj", "proj", LanguageNames.CSharp)
                .WithMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Standard));

            solution = project.Solution;

            for (var i = 0; i < markedSources.Length; i++)
            {
                var name = $"test{i + 1}.cs";
                var text = SourceText.From(ActiveStatementsDescription.ClearTags(markedSources[i]), Encoding.UTF8);
                var id = DocumentId.CreateNewId(project.Id, name);
                solution = solution.AddDocument(id, name, text, filePath: Path.Combine(TempRoot.Root, name));
            }

            await workspace.ChangeSolutionAsync(solution);
            return solution;
        }

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions1()
        {
            var markedSources = new[]
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
            var module3 = new Guid("33333333-3333-3333-3333-333333333333");
            var module4 = new Guid("44444444-4444-4444-4444-444444444444");

            var activeStatements = GetActiveStatementDebugInfosCSharp(
                markedSources,
                methodRowIds: new[] { 1, 2, 3, 4, 5 },
                ilOffsets: new[] { 1, 1, 1, 2, 3 },
                modules: new[] { module1, module1, module2, module2, module2 });

            // add an extra active statement that has no location, it should be ignored:
            activeStatements = activeStatements.Add(
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(new ManagedMethodId(module: Guid.NewGuid(), token: 0x06000005, version: 1), ilOffset: 10),
                    documentName: null,
                    sourceSpan: default,
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame));

            // add an extra active statement from project not belonging to the solution, it should be ignored:
            activeStatements = activeStatements.Add(
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(new ManagedMethodId(module: module3, token: 0x06000005, version: 1), ilOffset: 10),
                    "NonRoslynDocument.mcpp",
                    new SourceSpan(1, 1, 1, 10),
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame));

            // Add an extra active statement from language that doesn't support Roslyn EnC should be ignored:
            // See https://github.com/dotnet/roslyn/issues/24408 for test scenario.
            activeStatements = activeStatements.Add(
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(new ManagedMethodId(module: module4, token: 0x06000005, version: 1), ilOffset: 10),
                    "a.dummy",
                    new SourceSpan(2, 1, 2, 10),
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame));

            using var workspace = new TestWorkspace(composition: s_composition);

            var solution = await AddDefaultTestSolutionAsync(workspace, markedSources);
            var projectId = solution.ProjectIds.Single();
            var dummyProject = solution.AddProject("dummy_proj", "dummy_proj", NoCompilationConstants.LanguageName);
            solution = dummyProject.Solution.AddDocument(DocumentId.CreateNewId(dummyProject.Id, NoCompilationConstants.LanguageName), "a.dummy", "");
            var project = solution.GetProject(projectId);
            var document1 = project.Documents.Single(d => d.Name == "test1.cs");
            var document2 = project.Documents.Single(d => d.Name == "test2.cs");

            var editSession = CreateEditSession(solution, activeStatements);
            var baseActiveStatementsMap = await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            // Active Statements

            var statements = baseActiveStatementsMap.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();
            AssertEx.Equal(new[]
            {
                $"0: {document1.FilePath}: (9,14)-(9,35) flags=[LeafFrame, MethodUpToDate] mvid=11111111-1111-1111-1111-111111111111 0x06000001 v1 IL_0001",
                $"1: {document1.FilePath}: (4,32)-(4,37) flags=[MethodUpToDate, NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000002 v1 IL_0001",
                $"2: {document2.FilePath}: (21,14)-(21,24) flags=[MethodUpToDate, NonLeafFrame] mvid=22222222-2222-2222-2222-222222222222 0x06000003 v1 IL_0001",   // [|Test1.M1()|] in F2
                $"3: {document2.FilePath}: (8,20)-(8,25) flags=[MethodUpToDate, NonLeafFrame] mvid=22222222-2222-2222-2222-222222222222 0x06000004 v1 IL_0002",     // [|F2();|] in M2
                $"4: {document2.FilePath}: (26,20)-(26,25) flags=[MethodUpToDate, NonLeafFrame] mvid=22222222-2222-2222-2222-222222222222 0x06000005 v1 IL_0003",   // [|M2();|] in Main
                $"5: NonRoslynDocument.mcpp: (1,1)-(1,10) flags=[MethodUpToDate, NonLeafFrame] mvid={module3} 0x06000005 v1 IL_000A",
                $"6: a.dummy: (2,1)-(2,10) flags=[MethodUpToDate, NonLeafFrame] mvid={module4} 0x06000005 v1 IL_000A"
            }, statements.Select(InspectActiveStatementAndInstruction));

            // Active Statements per document

            Assert.Equal(4, baseActiveStatementsMap.DocumentPathMap.Count);

            AssertEx.Equal(new[]
            {
                $"1: {document1.FilePath}: (4,32)-(4,37) flags=[MethodUpToDate, NonLeafFrame]",
                $"0: {document1.FilePath}: (9,14)-(9,35) flags=[LeafFrame, MethodUpToDate]"
            }, baseActiveStatementsMap.DocumentPathMap[document1.FilePath].Select(InspectActiveStatement));

            AssertEx.Equal(new[]
            {
                $"3: {document2.FilePath}: (8,20)-(8,25) flags=[MethodUpToDate, NonLeafFrame]",            // [|F2();|] in M2
                $"2: {document2.FilePath}: (21,14)-(21,24) flags=[MethodUpToDate, NonLeafFrame]",          // [|Test1.M1()|] in F2
                $"4: {document2.FilePath}: (26,20)-(26,25) flags=[MethodUpToDate, NonLeafFrame]"           // [|M2();|] in Main
            }, baseActiveStatementsMap.DocumentPathMap[document2.FilePath].Select(InspectActiveStatement));

            AssertEx.Equal(new[]
            {
                $"5: NonRoslynDocument.mcpp: (1,1)-(1,10) flags=[MethodUpToDate, NonLeafFrame]",
            }, baseActiveStatementsMap.DocumentPathMap["NonRoslynDocument.mcpp"].Select(InspectActiveStatement));

            AssertEx.Equal(new[]
            {
                $"6: a.dummy: (2,1)-(2,10) flags=[MethodUpToDate, NonLeafFrame]",
            }, baseActiveStatementsMap.DocumentPathMap["a.dummy"].Select(InspectActiveStatement));

            // Exception Regions

            var analyzer = solution.GetProject(projectId).Services.GetRequiredService<IEditAndContinueAnalyzer>();
            var oldActiveStatements1 = await baseActiveStatementsMap.GetOldActiveStatementsAsync(analyzer, document1, CancellationToken.None).ConfigureAwait(false);

            AssertEx.Equal(new[]
            {
                $"[{document1.FilePath}: (4,8)-(4,46)]",
                "[]",
            }, oldActiveStatements1.Select(s => "[" + string.Join(", ", s.ExceptionRegions.Spans) + "]"));

            var oldActiveStatements2 = await baseActiveStatementsMap.GetOldActiveStatementsAsync(analyzer, document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"[{document2.FilePath}: (14,8)-(16,9), {document2.FilePath}: (10,10)-(12,11)]",
                "[]",
                $"[{document2.FilePath}: (26,35)-(26,46)]",
            }, oldActiveStatements2.Select(s => "[" + string.Join(", ", s.ExceptionRegions.Spans) + "]"));

            // GetActiveStatementAndExceptionRegionSpans

            // Assume 2 updates in Document2:
            //   Test2.M2: adding a line in front of try-catch.
            //   Test2.F2: moving the entire method 2 lines down.

            var newActiveStatementsInChangedDocuments = ImmutableArray.Create(
                new DocumentActiveStatementChanges(
                    oldSpans: oldActiveStatements2,
                    newStatements: ImmutableArray.Create(
                        statements[3].WithFileSpan(statements[3].FileSpan.AddLineDelta(+1)),
                        statements[2].WithFileSpan(statements[2].FileSpan.AddLineDelta(+2)),
                        statements[4]),
                    newExceptionRegions: ImmutableArray.Create(
                        oldActiveStatements2[0].ExceptionRegions.Spans.SelectAsArray(es => es.AddLineDelta(+1)),
                        oldActiveStatements2[1].ExceptionRegions.Spans,
                        oldActiveStatements2[2].ExceptionRegions.Spans)));

            EditSession.GetActiveStatementAndExceptionRegionSpans(
                module2,
                baseActiveStatementsMap,
                updatedMethodTokens: ImmutableArray.Create(0x06000004), // contains only recompiled methods in the project we are interested in (module2)
                previousNonRemappableRegions: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
                newActiveStatementsInChangedDocuments,
                out var activeStatementsInUpdatedMethods,
                out var nonRemappableRegions,
                out var exceptionRegionUpdates);

            AssertEx.Equal(new[]
            {
                $"0x06000004 v1 | AS {document2.FilePath}: (8,20)-(8,25) => (9,20)-(9,25)",
                $"0x06000004 v1 | ER {document2.FilePath}: (14,8)-(16,9) => (15,8)-(17,9)",
                $"0x06000004 v1 | ER {document2.FilePath}: (10,10)-(12,11) => (11,10)-(13,11)",
                $"0x06000003 v1 | AS {document2.FilePath}: (21,14)-(21,24) => (21,14)-(21,24)",
                $"0x06000005 v1 | AS {document2.FilePath}: (26,20)-(26,25) => (26,20)-(26,25)"
            }, nonRemappableRegions.Select(r => $"{r.Method.GetDebuggerDisplay()} | {r.Region.GetDebuggerDisplay()}"));

            AssertEx.Equal(new[]
            {
                $"0x06000004 v1 | (15,8)-(17,9) Delta=-1",
                $"0x06000004 v1 | (11,10)-(13,11) Delta=-1"
            }, exceptionRegionUpdates.Select(InspectExceptionRegionUpdate));

            AssertEx.Equal(new[]
            {
                $"0x06000004 v1 IL_0002: (9,20)-(9,25)"
            }, activeStatementsInUpdatedMethods.Select(InspectActiveStatementUpdate));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24439")]
        public async Task BaseActiveStatementsAndExceptionRegions2()
        {
            var baseSource =
@"class Test
{
    static void F1()
    {   
        try
        {
            <AS:0>F2();</AS:0>
        }
        catch (Exception) {
            Console.WriteLine(1);
            Console.WriteLine(2);
            Console.WriteLine(3);
        }
        /*insert1[1]*/
    }

    static void F2()
    {
        <AS:1>throw new Exception();</AS:1>
    }
}";
            var updatedSource = Update(baseSource, marker: "1");

            var module1 = new Guid("11111111-1111-1111-1111-111111111111");
            var baseText = SourceText.From(baseSource);
            var updatedText = SourceText.From(updatedSource);

            var baseActiveStatementInfos = GetActiveStatementDebugInfosCSharp(
                new[] { baseSource },
                modules: new[] { module1, module1 },
                methodVersions: new[] { 1, 1 },
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, // F1
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame,    // F2
                });

            using var workspace = new TestWorkspace(composition: s_composition);
            var solution = await AddDefaultTestSolutionAsync(workspace, new[] { baseSource });
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var editSession = CreateEditSession(solution, baseActiveStatementInfos);
            var baseActiveStatementMap = await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            // Active Statements

            var baseActiveStatements = baseActiveStatementMap.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();

            AssertEx.Equal(new[]
            {
                $"0: {document.FilePath}: (6,18)-(6,23) flags=[MethodUpToDate, NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000001 v1 IL_0000 '<AS:0>F2();</AS:0>'",
                $"1: {document.FilePath}: (18,14)-(18,36) flags=[LeafFrame, MethodUpToDate] mvid=11111111-1111-1111-1111-111111111111 0x06000002 v1 IL_0000 '<AS:1>throw new Exception();</AS:1>'"
            }, baseActiveStatements.Select(s => InspectActiveStatementAndInstruction(s, baseText)));

            // Exception Regions

            var analyzer = solution.GetProject(project.Id).Services.GetRequiredService<IEditAndContinueAnalyzer>();
            var oldActiveStatements = await baseActiveStatementMap.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None).ConfigureAwait(false);

            // Note that the spans correspond to the base snapshot (V2). 
            AssertEx.Equal(new[]
            {
                $"[{document.FilePath}: (8,8)-(12,9) 'catch (Exception) {{']",
                "[]",
            }, oldActiveStatements.Select(s => "[" + string.Join(", ", s.ExceptionRegions.Spans.Select(span => $"{span} '{GetFirstLineText(span.Span, baseText)}'")) + "]"));

            // GetActiveStatementAndExceptionRegionSpans

            var newActiveStatementsInChangedDocuments = ImmutableArray.Create(
                new DocumentActiveStatementChanges(
                    oldSpans: oldActiveStatements,
                    newStatements: ImmutableArray.Create(
                        baseActiveStatements[0],
                        baseActiveStatements[1].WithFileSpan(baseActiveStatements[1].FileSpan.AddLineDelta(+1))),
                    newExceptionRegions: ImmutableArray.Create(
                        oldActiveStatements[0].ExceptionRegions.Spans,
                        oldActiveStatements[1].ExceptionRegions.Spans))
            );

            EditSession.GetActiveStatementAndExceptionRegionSpans(
                module1,
                baseActiveStatementMap,
                updatedMethodTokens: ImmutableArray.Create(0x06000001), // F1
                previousNonRemappableRegions: ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>.Empty,
                newActiveStatementsInChangedDocuments,
                out var activeStatementsInUpdatedMethods,
                out var nonRemappableRegions,
                out var exceptionRegionUpdates);

            AssertEx.Equal(new[]
            {
                $"0x06000001 v1 | AS {document.FilePath}: (6,18)-(6,23) => (6,18)-(6,23)",
                $"0x06000001 v1 | ER {document.FilePath}: (8,8)-(12,9) => (8,8)-(12,9)",
                $"0x06000002 v1 | AS {document.FilePath}: (18,14)-(18,36) => (18,14)-(18,36)",
            }, nonRemappableRegions.OrderBy(r => r.Region.OldSpan.Span.Start.Line).Select(r => $"{r.Method.GetDebuggerDisplay()} | {r.Region.GetDebuggerDisplay()}"));

            AssertEx.Equal(new[]
            {
                "0x06000001 v1 | (8,8)-(12,9) Delta=0",
            }, exceptionRegionUpdates.Select(InspectExceptionRegionUpdate));

            AssertEx.Equal(new[]
            {
                "0x06000001 v1 IL_0000: (6,18)-(6,23) '<AS:0>F2();</AS:0>'"
            }, activeStatementsInUpdatedMethods.Select(update => $"{InspectActiveStatementUpdate(update)} '{GetFirstLineText(update.NewSpan.ToLinePositionSpan(), updatedText)}'"));
        }

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions_WithInitialNonRemappableRegions()
        {
            var markedSourceV1 =
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
    {   /*delete2
      */try
        {
        }
        <ER:1.0>catch
        {
            <AS:1>M();</AS:1>
        }</ER:1.0>/*insert2[1]*/
    }

    static void F3()
    {   
        try
        {
            try 
            {   /*delete1
              */<AS:2>M();</AS:2>/*insert1[3]*/
            }
            <ER:2.0>finally
            {
            }</ER:2.0>
        }
        <ER:2.1>catch
        {
        }</ER:2.1>
/*delete1

*/  }

    static void F4()
    {   /*insert1[1]*//*insert2[2]*/
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
        }</ER:3.1>
    }
}";
            var markedSourceV2 = Update(markedSourceV1, marker: "1");
            var markedSourceV3 = Update(markedSourceV2, marker: "2");

            var module1 = new Guid("11111111-1111-1111-1111-111111111111");
            var sourceTextV1 = SourceText.From(markedSourceV1);
            var sourceTextV2 = SourceText.From(markedSourceV2);
            var sourceTextV3 = SourceText.From(markedSourceV3);

            var activeStatementsPreRemap = GetActiveStatementDebugInfosCSharp(new[] { markedSourceV1 },
                modules: new[] { module1, module1, module1, module1 },
                methodVersions: new[] { 2, 2, 1, 1 }, // method F3 and F4 were not remapped
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, // F1
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, // F2
                    ActiveStatementFlags.None | ActiveStatementFlags.NonLeafFrame,           // F3
                    ActiveStatementFlags.None | ActiveStatementFlags.NonLeafFrame,           // F4
                });

            var exceptionSpans = ActiveStatementsDescription.GetExceptionRegions(markedSourceV1);

            var filePath = activeStatementsPreRemap[0].DocumentName;
            var spanPreRemap2 = new SourceFileSpan(filePath, activeStatementsPreRemap[2].SourceSpan.ToLinePositionSpan());
            var erPreRemap20 = new SourceFileSpan(filePath, sourceTextV1.Lines.GetLinePositionSpan(exceptionSpans[2][0]));
            var erPreRemap21 = new SourceFileSpan(filePath, sourceTextV1.Lines.GetLinePositionSpan(exceptionSpans[2][1]));
            var spanPreRemap3 = new SourceFileSpan(filePath, activeStatementsPreRemap[3].SourceSpan.ToLinePositionSpan());
            var erPreRemap30 = new SourceFileSpan(filePath, sourceTextV1.Lines.GetLinePositionSpan(exceptionSpans[3][0]));
            var erPreRemap31 = new SourceFileSpan(filePath, sourceTextV1.Lines.GetLinePositionSpan(exceptionSpans[3][1]));

            // Assume that the following edits have been made to F3 and F4 and set up non-remappable regions mapping
            // from the pre-remap spans of AS:2 and AS:3 to their current location.
            var initialNonRemappableRegions = new Dictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>>
            {
                { new ManagedMethodId(module1, 0x06000003, 1), ImmutableArray.Create(
                    // move AS:2 one line up:
                    new NonRemappableRegion(spanPreRemap2, spanPreRemap2.AddLineDelta(-1), isExceptionRegion: false),
                    // move ER:2.0 and ER:2.1 two lines down:
                    new NonRemappableRegion(erPreRemap20, erPreRemap20.AddLineDelta(+2), isExceptionRegion: true),
                    new NonRemappableRegion(erPreRemap21, erPreRemap21.AddLineDelta(+2), isExceptionRegion: true)) },
                { new ManagedMethodId(module1, 0x06000004, 1), ImmutableArray.Create(
                    // move AS:3 one line down:
                    new NonRemappableRegion(spanPreRemap3, spanPreRemap3.AddLineDelta(+1), isExceptionRegion: false),
                    // move ER:3.0 and ER:3.1 one line down:
                    new NonRemappableRegion(erPreRemap30, erPreRemap30.AddLineDelta(+1), isExceptionRegion: true),
                    new NonRemappableRegion(erPreRemap31, erPreRemap31.AddLineDelta(+1), isExceptionRegion: true)) }
            }.ToImmutableDictionary();

            using var workspace = new TestWorkspace(composition: s_composition);
            var solution = await AddDefaultTestSolutionAsync(workspace, new[] { markedSourceV2 });
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var editSession = CreateEditSession(solution, activeStatementsPreRemap, initialNonRemappableRegions);
            var baseActiveStatementMap = await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            // Active Statements

            var baseActiveStatements = baseActiveStatementMap.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();

            // Note that the spans of AS:2 and AS:3 correspond to the base snapshot (V2).
            AssertEx.Equal(new[]
            {
                $"0: {document.FilePath}: (6,18)-(6,22) flags=[MethodUpToDate, NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000001 v2 IL_0000 '<AS:0>M();</AS:0>'",
                $"1: {document.FilePath}: (20,18)-(20,22) flags=[MethodUpToDate, NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000002 v2 IL_0000 '<AS:1>M();</AS:1>'",
                $"2: {document.FilePath}: (29,22)-(29,26) flags=[NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000003 v1 IL_0000 '{{   <AS:2>M();</AS:2>'",
                $"3: {document.FilePath}: (53,22)-(53,26) flags=[NonLeafFrame] mvid=11111111-1111-1111-1111-111111111111 0x06000004 v1 IL_0000 '<AS:3>M();</AS:3>'"
            }, baseActiveStatements.Select(s => InspectActiveStatementAndInstruction(s, sourceTextV2)));

            // Exception Regions

            var analyzer = solution.GetProject(project.Id).Services.GetRequiredService<IEditAndContinueAnalyzer>();
            var oldActiveStatements = await baseActiveStatementMap.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None).ConfigureAwait(false);

            // Note that the spans correspond to the base snapshot (V2). 
            AssertEx.Equal(new[]
            {
                $"[{document.FilePath}: (8,16)-(10,9) '<ER:0.0>catch']",
                $"[{document.FilePath}: (18,16)-(21,9) '<ER:1.0>catch']",
                $"[{document.FilePath}: (38,16)-(40,9) '<ER:2.1>catch', {document.FilePath}: (34,20)-(36,13) '<ER:2.0>finally']",
                $"[{document.FilePath}: (56,16)-(58,9) '<ER:3.1>catch', {document.FilePath}: (51,20)-(54,13) '<ER:3.0>catch']",
            }, oldActiveStatements.Select(s => "[" + string.Join(", ", s.ExceptionRegions.Spans.Select(span => $"{span} '{GetFirstLineText(span.Span, sourceTextV2)}'")) + "]"));

            // GetActiveStatementAndExceptionRegionSpans

            // Assume 2 more updates:
            //   F2: Move 'try' one line up (a new non-remappable entries will be added)
            //   F4: Insert 2 new lines before the first 'try' (an existing non-remappable entries will be updated)
            var newActiveStatementsInChangedDocuments = ImmutableArray.Create(
                new DocumentActiveStatementChanges(
                    oldSpans: oldActiveStatements,
                    newStatements: ImmutableArray.Create(
                        baseActiveStatements[0],
                        baseActiveStatements[1].WithFileSpan(baseActiveStatements[1].FileSpan.AddLineDelta(-1)),
                        baseActiveStatements[2],
                        baseActiveStatements[3].WithFileSpan(baseActiveStatements[3].FileSpan.AddLineDelta(+2))),
                    newExceptionRegions: ImmutableArray.Create(
                        oldActiveStatements[0].ExceptionRegions.Spans,
                        oldActiveStatements[1].ExceptionRegions.Spans.SelectAsArray(es => es.AddLineDelta(-1)),
                        oldActiveStatements[2].ExceptionRegions.Spans,
                        oldActiveStatements[3].ExceptionRegions.Spans.SelectAsArray(es => es.AddLineDelta(+2)))));

            EditSession.GetActiveStatementAndExceptionRegionSpans(
                module1,
                baseActiveStatementMap,
                updatedMethodTokens: ImmutableArray.Create(0x06000002, 0x06000004), // F2, F4
                initialNonRemappableRegions,
                newActiveStatementsInChangedDocuments,
                out var activeStatementsInUpdatedMethods,
                out var nonRemappableRegions,
                out var exceptionRegionUpdates);

            // Note: Since no method have been remapped yet all the following spans are in their pre-remap locations: 
            AssertEx.Equal(new[]
            {
                $"0x06000001 v2 | AS {document.FilePath}: (6,18)-(6,22) => (6,18)-(6,22)",
                $"0x06000002 v2 | ER {document.FilePath}: (18,16)-(21,9) => (17,16)-(20,9)",
                $"0x06000002 v2 | AS {document.FilePath}: (20,18)-(20,22) => (19,18)-(19,22)",
                $"0x06000003 v1 | AS {document.FilePath}: (30,22)-(30,26) => (29,22)-(29,26)",  // AS:2 moved -1 in first edit, 0 in second
                $"0x06000003 v1 | ER {document.FilePath}: (32,20)-(34,13) => (34,20)-(36,13)",  // ER:2.0 moved +2 in first edit, 0 in second
                $"0x06000003 v1 | ER {document.FilePath}: (36,16)-(38,9) => (38,16)-(40,9)",    // ER:2.0 moved +2 in first edit, 0 in second
                $"0x06000004 v1 | ER {document.FilePath}: (50,20)-(53,13) => (53,20)-(56,13)",  // ER:3.0 moved +1 in first edit, +2 in second              
                $"0x06000004 v1 | AS {document.FilePath}: (52,22)-(52,26) => (55,22)-(55,26)",  // AS:3 moved +1 in first edit, +2 in second
                $"0x06000004 v1 | ER {document.FilePath}: (55,16)-(57,9) => (58,16)-(60,9)",    // ER:3.1 moved +1 in first edit, +2 in second     
            }, nonRemappableRegions.OrderBy(r => r.Region.OldSpan.Span.Start.Line).Select(r => $"{r.Method.GetDebuggerDisplay()} | {r.Region.GetDebuggerDisplay()}"));

            AssertEx.Equal(new[]
            {
                $"0x06000002 v2 | (17,16)-(20,9) Delta=1",
                $"0x06000003 v1 | (34,20)-(36,13) Delta=-2",
                $"0x06000003 v1 | (38,16)-(40,9) Delta=-2",
                $"0x06000004 v1 | (53,20)-(56,13) Delta=-3",
                $"0x06000004 v1 | (58,16)-(60,9) Delta=-3",
            }, exceptionRegionUpdates.OrderBy(r => r.NewSpan.StartLine).Select(InspectExceptionRegionUpdate));

            AssertEx.Equal(new[]
            {
                $"0x06000002 v2 IL_0000: (19,18)-(19,22) '<AS:1>M();</AS:1>'",
                $"0x06000004 v1 IL_0000: (55,22)-(55,26) '<AS:3>M();</AS:3>'"
            }, activeStatementsInUpdatedMethods.Select(update => $"{InspectActiveStatementUpdate(update)} '{GetFirstLineText(update.NewSpan.ToLinePositionSpan(), sourceTextV3)}'"));
        }

        [Fact]
        public async Task BaseActiveStatementsAndExceptionRegions_Recursion()
        {
            var markedSources = new[]
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

            var activeStatements = GetActiveStatementDebugInfosCSharp(
                markedSources,
                methodRowIds: new[] { 1, 2 },
                ilOffsets: new[] { 1, 1 },
                flags: new[]
                {
                    ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.NonUserCode | ActiveStatementFlags.PartiallyExecuted | ActiveStatementFlags.MethodUpToDate,
                    ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.LeafFrame | ActiveStatementFlags.MethodUpToDate
                });

            using var workspace = new TestWorkspace(composition: s_composition);
            var solution = await AddDefaultTestSolutionAsync(workspace, markedSources);
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var editSession = CreateEditSession(solution, activeStatements);
            var baseActiveStatementMap = await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            // Active Statements

            Assert.Equal(1, baseActiveStatementMap.DocumentPathMap.Count);

            AssertEx.Equal(new[]
            {
                $"1: {document.FilePath}: (6,18)-(6,22) flags=[LeafFrame, MethodUpToDate, NonLeafFrame]",
                $"0: {document.FilePath}: (15,14)-(15,18) flags=[PartiallyExecuted, NonUserCode, MethodUpToDate, NonLeafFrame]",
            }, baseActiveStatementMap.DocumentPathMap[document.FilePath].Select(InspectActiveStatement));

            Assert.Equal(2, baseActiveStatementMap.InstructionMap.Count);

            var statements = baseActiveStatementMap.InstructionMap.Values.OrderBy(v => v.InstructionId.Method.Token).ToArray();
            var s = statements[0];
            Assert.Equal(0x06000001, s.InstructionId.Method.Token);
            Assert.Equal(document.FilePath, s.FilePath);
            Assert.True(s.IsNonLeaf);

            s = statements[1];
            Assert.Equal(0x06000002, s.InstructionId.Method.Token);
            Assert.Equal(document.FilePath, s.FilePath);
            Assert.True(s.IsLeaf);
            Assert.True(s.IsNonLeaf);

            // Exception Regions

            var analyzer = solution.GetProject(project.Id).Services.GetRequiredService<IEditAndContinueAnalyzer>();
            var oldActiveStatements = await baseActiveStatementMap.GetOldActiveStatementsAsync(analyzer, document, CancellationToken.None).ConfigureAwait(false);

            AssertEx.Equal(new[]
            {
                $"[{document.FilePath}: (8,8)-(10,9)]",
                "[]"
            }, oldActiveStatements.Select(s => "[" + string.Join(",", s.ExceptionRegions.Spans) + "]"));
        }
    }
}
