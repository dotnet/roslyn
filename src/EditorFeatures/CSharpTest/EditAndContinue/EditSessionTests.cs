// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public class EditSessionTests
    {
        internal sealed class TestActiveStatementProvider : IActiveStatementProvider
        {
            private readonly ImmutableArray<ActiveStatementDebugInfo> _infos;

            public TestActiveStatementProvider(ImmutableArray<ActiveStatementDebugInfo> infos) 
                => _infos = infos;

            public Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(_infos);
        }

        internal static ImmutableArray<ActiveStatementDebugInfo> GetActiveStatementDebugInfos(string[] markedSources, int[] methodRowIds, string extension)
        {
            IEnumerable<ActiveStatementDebugInfo> Enumerate()
            {
                int id = 0;
                int sourceIndex = 0;
                foreach (var markedSource in markedSources)
                {
                    foreach (var activeStatement in ActiveStatementsDescription.GetActiveStatements(markedSource))
                    {
                        yield return new ActiveStatementDebugInfo(
                            id, 
                            new ActiveInstructionId(default, methodToken: 0x06000000 | methodRowIds[id], 1, 0), 
                            documentName: TestWorkspace.GetDefaultTestSourceDocumentName(sourceIndex, extension), 
                            activeStatement.Span, 
                            activeStatement.Flags);

                        id++;
                    }

                    sourceIndex++;
                }
            }

            return Enumerate().ToImmutableArray();
        }

        [Fact]
        public async Task ActiveStatementsAndExceptionRegions()
        {
            var markedSource = new[]
            {
@"class C
{
    static void M1()
    {
        <AS:1>F1();</AS:1>
    }

    static void F1()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}",
@"class D
{
    static void M2()
    {
        try
        {
          try
          {
              <AS:1>F2();</AS:1>
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
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
"
            };

            var activeStatements = GetActiveStatementDebugInfos(markedSource, methodRowIds: new[] { 1, 2, 3, 4 }, ".cs").Concat(
                new ActiveStatementDebugInfo(5, new ActiveInstructionId(default, 0x06000001, 1, 0), TestWorkspace.GetDefaultTestSourceDocumentName(0, ".cs"), default, ActiveStatementFlags.NonUserCode));

            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(typeof(CSharpEditAndContinueAnalyzer)));

            using (var workspace = TestWorkspace.CreateCSharp(
                ActiveStatementsDescription.ClearTags(markedSource),
                exportProvider: exportProvider))
            {
                var baseSolution = workspace.CurrentSolution;

                var docs = baseSolution.Projects.Single().Documents.ToArray();

                var debuggingSession = new DebuggingSession(baseSolution);

                var activeStatementProvider = new TestActiveStatementProvider(activeStatements);

                var editSession = new EditSession(
                    baseSolution,
                    debuggingSession,
                    activeStatementProvider,
                    ImmutableDictionary<ProjectId, ProjectReadOnlyReason>.Empty,
                    SpecializedCollections.EmptyReadOnlyDictionary<ActiveInstructionId, LinePositionSpan>(),
                    stoppedAtException: false);

                // Active Statements

                var baseActiveStatements = await editSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.Equal(2, baseActiveStatements.DocumentMap.Count);

                string InspectAS(ActiveStatement statement) 
                    => statement.Span + (statement.Flags != default ? $"[{statement.Flags}]" : "");

                AssertEx.Equal(new[] { "(9,14)-(9,35)[LeafFrame]", "(4,14)-(4,19)" }, baseActiveStatements.DocumentMap[docs[0].Id].Select(InspectAS));
                AssertEx.Equal(new[] { "(21,14)-(21,35)[LeafFrame]", "(8,20)-(8,25)" }, baseActiveStatements.DocumentMap[docs[1].Id].Select(InspectAS));

                Assert.Equal(4, baseActiveStatements.Ids.Count);

                var s = baseActiveStatements.Ids[0];
                Assert.Equal(0x06000001, s.InstructionId.MethodToken);
                Assert.Equal(0, s.Ordinal);
                Assert.Equal(docs[0].Id, s.DocumentId);

                s = baseActiveStatements.Ids[1];
                Assert.Equal(0x06000002, s.InstructionId.MethodToken);
                Assert.Equal(1, s.Ordinal);
                Assert.Equal(docs[0].Id, s.DocumentId);

                s = baseActiveStatements.Ids[2];
                Assert.Equal(0x06000003, s.InstructionId.MethodToken);
                Assert.Equal(0, s.Ordinal);
                Assert.Equal(docs[1].Id, s.DocumentId);

                s = baseActiveStatements.Ids[3];
                Assert.Equal(0x06000004, s.InstructionId.MethodToken);
                Assert.Equal(1, s.Ordinal);
                Assert.Equal(docs[1].Id, s.DocumentId);

                // Exception Regions

                var baseExceptionRegions = await editSession.BaseActiveExceptionRegions.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

                AssertEx.Equal(new[] { "3.0: (14,8)-(16,9)", "3.1: (10,10)-(12,11)" }, baseExceptionRegions.Select(r => $"{r.ActiveStatementDebuggerId}.{r.Ordinal}: {r.Span}"));
            }
        }
    }
}
