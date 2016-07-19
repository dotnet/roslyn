// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal class CodeAnalysisService : ServiceHubJsonRpcServiceBase
    {
        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        public async Task CalculateDiagnosticsAsync(byte[] checksum, Guid guid, string debugName, byte[][] hostAnalyzerChecksums, string[] analyzerIds, string streamName)
        {
            // entry point for diagnostic service
            var solutionSnapshotId = await RoslynServices.AssetService.GetAssetAsync<SolutionSnapshotId>(new Checksum(ImmutableArray.Create(checksum))).ConfigureAwait(false);
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);

            var solution = await RoslynServices.SolutionService.GetSolutionAsync(solutionSnapshotId, CancellationToken).ConfigureAwait(false);

            var analyzers = new List<AnalyzerReference>();
            foreach (var analyzerChecksum in hostAnalyzerChecksums)
            {
                analyzers.Add(await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(new Checksum(ImmutableArray.Create(analyzerChecksum))).ConfigureAwait(false));
            }

            var result = await (new DiagnosticComputer()).GetDiagnosticsAsync(solution, projectId, analyzers, analyzerIds, CancellationToken).ConfigureAwait(false);

            var diagnosticSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

            using (var stream = new SlaveDirectStream(streamName))
            {
                await stream.ConnectAsync(CancellationToken).ConfigureAwait(false);

                using (var writer = new ObjectWriter(stream))
                {
                    writer.WriteInt32(result.AnalysisResult.Count);

                    foreach (var kv in result.AnalysisResult)
                    {
                        writer.WriteString(kv.Key);

                        writer.WriteInt32(kv.Value.SyntaxLocals.Count);
                        foreach (var entry in kv.Value.SyntaxLocals)
                        {
                            Serializer.Serialize(entry.Key, writer, CancellationToken);
                            diagnosticSerializer.WriteTo(writer, entry.Value, CancellationToken);
                        }

                        writer.WriteInt32(kv.Value.SemanticLocals.Count);
                        foreach (var entry in kv.Value.SemanticLocals)
                        {
                            Serializer.Serialize(entry.Key, writer, CancellationToken);
                            diagnosticSerializer.WriteTo(writer, entry.Value, CancellationToken);
                        }

                        writer.WriteInt32(kv.Value.NonLocals.Count);
                        foreach (var entry in kv.Value.NonLocals)
                        {
                            Serializer.Serialize(entry.Key, writer, CancellationToken);
                            diagnosticSerializer.WriteTo(writer, entry.Value, CancellationToken);
                        }

                        diagnosticSerializer.WriteTo(writer, kv.Value.Others, CancellationToken);
                    }

                    writer.WriteInt32(result.TelemetryInfo.Count);

                    foreach (var kv in result.TelemetryInfo)
                    {
                        writer.WriteString(kv.Key);

                        writer.WriteInt32(kv.Value.CompilationStartActionsCount);
                        writer.WriteInt32(kv.Value.CompilationEndActionsCount);
                        writer.WriteInt32(kv.Value.CompilationActionsCount);
                        writer.WriteInt32(kv.Value.SyntaxTreeActionsCount);
                        writer.WriteInt32(kv.Value.SemanticModelActionsCount);
                        writer.WriteInt32(kv.Value.SymbolActionsCount);
                        writer.WriteInt32(kv.Value.SyntaxNodeActionsCount);
                        writer.WriteInt32(kv.Value.CodeBlockStartActionsCount);
                        writer.WriteInt32(kv.Value.CodeBlockEndActionsCount);
                        writer.WriteInt32(kv.Value.CodeBlockActionsCount);
                        writer.WriteInt32(kv.Value.OperationActionsCount);
                        writer.WriteInt32(kv.Value.OperationBlockActionsCount);
                        writer.WriteInt32(kv.Value.OperationBlockStartActionsCount);
                        writer.WriteInt32(kv.Value.OperationBlockEndActionsCount);
                        writer.WriteInt64(kv.Value.ExecutionTime.Ticks);
                    }
                }

                await stream.FlushAsync(CancellationToken).ConfigureAwait(false);

                // TODO: think of a way this is not needed
                // wait for the other side to finish reading data I sent over
                stream.WaitForMaster();
            }
        }
    }
}
