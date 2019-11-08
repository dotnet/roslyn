// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias hub;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Newtonsoft.Json;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class JsonConverterTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestChecksum()
        {
            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            VerifyJsonSerialization(checksum);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestChecksum_Null()
        {
            VerifyJsonSerialization<Checksum>(null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestChecksumNull()
        {
            VerifyJsonSerialization(Checksum.Null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestSolutionId()
        {
            VerifyJsonSerialization(SolutionId.CreateNewId("solution"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestProjectId()
        {
            VerifyJsonSerialization(ProjectId.CreateNewId("project"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestDocumentId()
        {
            VerifyJsonSerialization(DocumentId.CreateNewId(ProjectId.CreateNewId("project"), "document"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestDiagnosticArguments()
        {
            var arguments = new DiagnosticArguments(
                forcedAnalysis: false,
                reportSuppressedDiagnostics: true,
                logAnalyzerExecutionTime: false,
                projectId: ProjectId.CreateNewId("project"),
                optionSetChecksum: Checksum.Null,
                analyzerIds: new[] { "analyzer1", "analyzer2" });

            VerifyJsonSerialization(arguments, (x, y) =>
            {
                if (x is
                {
                    ForcedAnalysis: y.ForcedAnalysis,
                    ReportSuppressedDiagnostics: y.ReportSuppressedDiagnostics,
                    LogAnalyzerExecutionTime: y.LogAnalyzerExecutionTime,
                    ProjectId: y.ProjectId,
                    OptionSetChecksum: y.OptionSetChecksum,
                    AnalyzerIds: { Length: y.AnalyzerIds.Length }
                }
&& x.AnalyzerIds.Except(y.AnalyzerIds).Count() is 0)
                {
                    return 0;
                }

                return 1;
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestTextSpan()
        {
            VerifyJsonSerialization(new TextSpan(10, 5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestSymbolKey()
        {
            VerifyJsonSerialization(new SymbolKey("TEST"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestTodoCommentDescriptor()
        {
            VerifyJsonSerialization(new TodoCommentDescriptor("Test", 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestTodoComment()
        {
            VerifyJsonSerialization(new TodoComment(new TodoCommentDescriptor("Test", 1), "Message", 10));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestTodoCommentDescriptorImmutableArray()
        {
            VerifyJsonSerialization(ImmutableArray.Create(new TodoCommentDescriptor("Test", 0), new TodoCommentDescriptor("Test1", 1)), (x, y) =>
            {
                return x.SequenceEqual(y) ? 0 : 1;
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestTodoCommentList()
        {
            VerifyJsonSerialization(new[] {
                new TodoComment(new TodoCommentDescriptor("Test1", 1), "Message1", 10),
                new TodoComment(new TodoCommentDescriptor("Test2", 2), "Message2", 20)}.ToList(), (x, y) =>
                {
                    return x.SequenceEqual(y) ? 0 : 1;
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestPinnedSolutionInfo()
        {
            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            VerifyJsonSerialization(new PinnedSolutionInfo(scopeId: 10, fromPrimaryBranch: false, workspaceVersion: 100, solutionChecksum: checksum), (x, y) =>
            {
                return (x.ScopeId == y.ScopeId &&
                        x.FromPrimaryBranch == y.FromPrimaryBranch &&
                        x.WorkspaceVersion == y.WorkspaceVersion &&
                        x.SolutionChecksum == y.SolutionChecksum) ? 0 : 1;
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestAnalyzerPerformanceInfo()
        {
            VerifyJsonSerialization(
                new AnalyzerPerformanceInfo("testAnalyzer", builtIn: false, timeSpan: TimeSpan.FromMilliseconds(12345)),
                (x, y) => (x.AnalyzerId == y.AnalyzerId && x.BuiltIn == y.BuiltIn && x.TimeSpan == y.TimeSpan) ? 0 : 1);
        }

        private static void VerifyJsonSerialization<T>(T value, Comparison<T> equality = null)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(hub::Microsoft.CodeAnalysis.Remote.AggregateJsonConverter.Instance);

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, value);

                using (var reader = new JsonTextReader(new StringReader(writer.ToString())))
                {
                    var deserialized = serializer.Deserialize<T>(reader);

                    if (equality != null)
                    {
                        Assert.Equal(0, equality(value, deserialized));
                        return;
                    }

                    Assert.Equal(value, deserialized);
                }
            }
        }
    }
}
