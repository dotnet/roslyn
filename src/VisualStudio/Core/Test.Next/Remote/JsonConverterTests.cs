// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias hub;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class JsonConverterTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestChecksum()
        {
            VerifyJsonSerialization(new Checksum(Guid.NewGuid().ToByteArray()));
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
            var arguments = new hub::Microsoft.CodeAnalysis.Remote.Diagnostics.DiagnosticArguments(
                reportSuppressedDiagnostics: true,
                logAnalyzerExecutionTime: false,
                projectId: ProjectId.CreateNewId("project"),
                optionSetChecksum: Checksum.Null,
                hostAnalyzerChecksums: ImmutableArray.CreateRange(new[] { new Checksum(Guid.NewGuid().ToByteArray()), new Checksum(Guid.NewGuid().ToByteArray()) }),
                analyzerIds: new[] { "analyzer1", "analyzer2" });

            VerifyJsonSerialization(arguments, (x, y) =>
            {
                if (x.ReportSuppressedDiagnostics == y.ReportSuppressedDiagnostics &&
                    x.LogAnalyzerExecutionTime == y.LogAnalyzerExecutionTime &&
                    x.ProjectId == y.ProjectId &&
                    x.OptionSetChecksum == y.OptionSetChecksum &&
                    x.HostAnalyzerChecksums.Length == y.HostAnalyzerChecksums.Length &&
                    x.HostAnalyzerChecksums.Except(y.HostAnalyzerChecksums).Count() == 0 &&
                    x.AnalyzerIds.Length == y.AnalyzerIds.Length &&
                    x.AnalyzerIds.Except(y.AnalyzerIds).Count() == 0)
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