// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticResultSerializer
    {
        public static (int diagnostics, int telemetry, int exceptions) Serialize(
            ObjectWriter writer, DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder> result, CancellationToken cancellationToken)
        {
            var diagnosticCount = 0;
            var diagnosticSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

            var analysisResult = result.AnalysisResult;

            writer.WriteInt32(analysisResult.Count);
            foreach (var kv in analysisResult)
            {
                writer.WriteString(kv.Key);

                diagnosticCount += Serialize(writer, diagnosticSerializer, kv.Value.SyntaxLocals, cancellationToken);
                diagnosticCount += Serialize(writer, diagnosticSerializer, kv.Value.SemanticLocals, cancellationToken);
                diagnosticCount += Serialize(writer, diagnosticSerializer, kv.Value.NonLocals, cancellationToken);

                diagnosticSerializer.WriteTo(writer, kv.Value.Others, cancellationToken);
                diagnosticCount += kv.Value.Others.Length;
            }

            var telemetryInfo = result.TelemetryInfo;

            writer.WriteInt32(telemetryInfo.Count);
            foreach (var kv in telemetryInfo)
            {
                writer.WriteString(kv.Key);
                Serialize(writer, kv.Value, cancellationToken);
            }

            var exceptions = result.Exceptions;

            writer.WriteInt32(exceptions.Count);
            foreach (var kv in exceptions)
            {
                writer.WriteString(kv.Key);
                diagnosticSerializer.WriteTo(writer, kv.Value, cancellationToken);
            }

            // report how many data has been sent
            return (diagnosticCount, telemetryInfo.Count, exceptions.Count);
        }

        public static DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult> Deserialize(
            ObjectReader reader, IDictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, VersionStamp version, CancellationToken cancellationToken)
        {
            var diagnosticDataSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

            var analysisMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();

            var analysisCount = reader.ReadInt32();
            for (var i = 0; i < analysisCount; i++)
            {
                var analyzer = analyzerMap[reader.ReadString()];

                var syntaxLocalMap = Deserialize(reader, diagnosticDataSerializer, project, cancellationToken);
                var semanticLocalMap = Deserialize(reader, diagnosticDataSerializer, project, cancellationToken);
                var nonLocalMap = Deserialize(reader, diagnosticDataSerializer, project, cancellationToken);

                var others = diagnosticDataSerializer.ReadFrom(reader, project, cancellationToken);

                var analysisResult = DiagnosticAnalysisResult.CreateFromSerialization(
                    project,
                    version,
                    syntaxLocalMap,
                    semanticLocalMap,
                    nonLocalMap,
                    GetOrDefault(others));

                analysisMap.Add(analyzer, analysisResult);
            }

            var telemetryMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();

            var telemetryCount = reader.ReadInt32();
            for (var i = 0; i < telemetryCount; i++)
            {
                var analyzer = analyzerMap[reader.ReadString()];
                var telemetryInfo = Deserialize(reader, cancellationToken);

                telemetryMap.Add(analyzer, telemetryInfo);
            }

            var exceptionMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>();

            var exceptionCount = reader.ReadInt32();
            for (var i = 0; i < exceptionCount; i++)
            {
                var analyzer = analyzerMap[reader.ReadString()];
                var exceptions = diagnosticDataSerializer.ReadFrom(reader, project, cancellationToken);

                exceptionMap.Add(analyzer, GetOrDefault(exceptions));
            }

            return DiagnosticAnalysisResultMap.Create(analysisMap.ToImmutable(), telemetryMap.ToImmutable(), exceptionMap.ToImmutable());
        }

        private static int Serialize(
            ObjectWriter writer,
            DiagnosticDataSerializer serializer,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> diagnostics,
            CancellationToken cancellationToken)
        {
            var count = 0;

            writer.WriteInt32(diagnostics.Count);
            foreach (var kv in diagnostics)
            {
                kv.Key.WriteTo(writer);
                serializer.WriteTo(writer, kv.Value, cancellationToken);

                count += kv.Value.Length;
            }

            return count;
        }

        private static ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Deserialize(
            ObjectReader reader,
            DiagnosticDataSerializer serializer,
            Project project,
            CancellationToken cancellationToken)
        {
            var count = reader.ReadInt32();
            var map = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
            for (var i = 0; i < count; i++)
            {
                var documentId = DocumentId.ReadFrom(reader);
                var document = project.GetDocument(documentId);

                var diagnostics = serializer.ReadFrom(reader, document, cancellationToken);

                if (document?.SupportsDiagnostics() == false)
                {
                    // drop diagnostics for non-null document that doesn't support
                    // diagnostics
                    continue;
                }

                map.Add(documentId, GetOrDefault(diagnostics));
            }

            return map.ToImmutable();
        }

        private static void Serialize(ObjectWriter writer, AnalyzerTelemetryInfo telemetryInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32(telemetryInfo.CompilationStartActionsCount);
            writer.WriteInt32(telemetryInfo.CompilationEndActionsCount);
            writer.WriteInt32(telemetryInfo.CompilationActionsCount);
            writer.WriteInt32(telemetryInfo.SyntaxTreeActionsCount);
            writer.WriteInt32(telemetryInfo.SemanticModelActionsCount);
            writer.WriteInt32(telemetryInfo.SymbolActionsCount);
            writer.WriteInt32(telemetryInfo.SymbolStartActionsCount);
            writer.WriteInt32(telemetryInfo.SymbolEndActionsCount);
            writer.WriteInt32(telemetryInfo.SyntaxNodeActionsCount);
            writer.WriteInt32(telemetryInfo.CodeBlockStartActionsCount);
            writer.WriteInt32(telemetryInfo.CodeBlockEndActionsCount);
            writer.WriteInt32(telemetryInfo.CodeBlockActionsCount);
            writer.WriteInt32(telemetryInfo.OperationActionsCount);
            writer.WriteInt32(telemetryInfo.OperationBlockActionsCount);
            writer.WriteInt32(telemetryInfo.OperationBlockStartActionsCount);
            writer.WriteInt32(telemetryInfo.OperationBlockEndActionsCount);
            writer.WriteInt32(telemetryInfo.SuppressionActionsCount);
            writer.WriteInt64(telemetryInfo.ExecutionTime.Ticks);
            writer.WriteBoolean(telemetryInfo.Concurrent);
        }

        private static AnalyzerTelemetryInfo Deserialize(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilationStartActionsCount = reader.ReadInt32();
            var compilationEndActionsCount = reader.ReadInt32();
            var compilationActionsCount = reader.ReadInt32();
            var syntaxTreeActionsCount = reader.ReadInt32();
            var semanticModelActionsCount = reader.ReadInt32();
            var symbolActionsCount = reader.ReadInt32();
            var symbolStartActionsCount = reader.ReadInt32();
            var symbolEndActionsCount = reader.ReadInt32();
            var syntaxNodeActionsCount = reader.ReadInt32();
            var codeBlockStartActionsCount = reader.ReadInt32();
            var codeBlockEndActionsCount = reader.ReadInt32();
            var codeBlockActionsCount = reader.ReadInt32();
            var operationActionsCount = reader.ReadInt32();
            var operationBlockActionsCount = reader.ReadInt32();
            var operationBlockStartActionsCount = reader.ReadInt32();
            var operationBlockEndActionsCount = reader.ReadInt32();
            var suppressionActionsCount = reader.ReadInt32();
            var executionTime = new TimeSpan(reader.ReadInt64());
            var concurrent = reader.ReadBoolean();

            return new AnalyzerTelemetryInfo()
            {
                CompilationStartActionsCount = compilationStartActionsCount,
                CompilationEndActionsCount = compilationEndActionsCount,
                CompilationActionsCount = compilationActionsCount,

                SyntaxTreeActionsCount = syntaxTreeActionsCount,
                SemanticModelActionsCount = semanticModelActionsCount,
                SymbolActionsCount = symbolActionsCount,
                SymbolStartActionsCount = symbolStartActionsCount,
                SymbolEndActionsCount = symbolEndActionsCount,
                SyntaxNodeActionsCount = syntaxNodeActionsCount,

                CodeBlockStartActionsCount = codeBlockStartActionsCount,
                CodeBlockEndActionsCount = codeBlockEndActionsCount,
                CodeBlockActionsCount = codeBlockActionsCount,

                OperationActionsCount = operationActionsCount,
                OperationBlockStartActionsCount = operationBlockStartActionsCount,
                OperationBlockEndActionsCount = operationBlockEndActionsCount,
                OperationBlockActionsCount = operationBlockActionsCount,

                SuppressionActionsCount = suppressionActionsCount,

                ExecutionTime = executionTime,

                Concurrent = concurrent
            };
        }

        private static ImmutableArray<T> GetOrDefault<T>(StrongBox<ImmutableArray<T>> items)
        {
            return items?.Value ?? default;
        }
    }
}
