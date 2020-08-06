// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticResultSerializer
    {
        public static (int diagnostics, int telemetry) WriteDiagnosticAnalysisResults(
            ObjectWriter writer,
            AnalysisKind? analysisKind,
            DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder> result,
            CancellationToken cancellationToken)
        {
            var diagnosticCount = 0;
            var diagnosticSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

            writer.WriteInt32(result.AnalysisResult.Count);
            foreach (var (analyzerId, analyzerResults) in result.AnalysisResult)
            {
                writer.WriteString(analyzerId);

                switch (analysisKind)
                {
                    case AnalysisKind.Syntax:
                        diagnosticCount += WriteDiagnosticDataMap(writer, diagnosticSerializer, analyzerResults.SyntaxLocals, cancellationToken);
                        break;

                    case AnalysisKind.Semantic:
                        diagnosticCount += WriteDiagnosticDataMap(writer, diagnosticSerializer, analyzerResults.SemanticLocals, cancellationToken);
                        break;

                    case null:
                        diagnosticCount += WriteDiagnosticDataMap(writer, diagnosticSerializer, analyzerResults.SyntaxLocals, cancellationToken);
                        diagnosticCount += WriteDiagnosticDataMap(writer, diagnosticSerializer, analyzerResults.SemanticLocals, cancellationToken);
                        diagnosticCount += WriteDiagnosticDataMap(writer, diagnosticSerializer, analyzerResults.NonLocals, cancellationToken);

                        diagnosticSerializer.WriteDiagnosticData(writer, analyzerResults.Others, cancellationToken);
                        diagnosticCount += analyzerResults.Others.Length;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(analysisKind.Value);
                }
            }

            writer.WriteInt32(result.TelemetryInfo.Count);
            foreach (var (analyzerId, analyzerTelemetry) in result.TelemetryInfo)
            {
                writer.WriteString(analyzerId);
                WriteTelemetry(writer, analyzerTelemetry, cancellationToken);
            }

            // report how many data has been sent
            return (diagnosticCount, result.TelemetryInfo.Count);
        }

        public static bool TryReadDiagnosticAnalysisResults(
            ObjectReader reader,
            IDictionary<string, DiagnosticAnalyzer> analyzerMap,
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            VersionStamp version,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>? result)
        {
            result = null;

            try
            {
                var diagnosticDataSerializer = new DiagnosticDataSerializer(VersionStamp.Default, VersionStamp.Default);

                var analysisMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
                var documentIds = documentAnalysisScope != null ? ImmutableHashSet.Create(documentAnalysisScope.TextDocument.Id) : null;

                var analysisCount = reader.ReadInt32();
                for (var i = 0; i < analysisCount; i++)
                {
                    var analyzer = analyzerMap[reader.ReadString()];

                    DiagnosticAnalysisResult analysisResult;
                    if (documentAnalysisScope != null)
                    {
                        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? syntaxLocalMap, semanticLocalMap;
                        if (documentAnalysisScope.Kind == AnalysisKind.Syntax)
                        {
                            if (!TryReadDiagnosticDataMap(reader, diagnosticDataSerializer, project, cancellationToken, out syntaxLocalMap))
                            {
                                return false;
                            }

                            semanticLocalMap = ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty;
                        }
                        else
                        {
                            Debug.Assert(documentAnalysisScope.Kind == AnalysisKind.Semantic);
                            if (!TryReadDiagnosticDataMap(reader, diagnosticDataSerializer, project, cancellationToken, out semanticLocalMap))
                            {
                                return false;
                            }

                            syntaxLocalMap = ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty;
                        }

                        analysisResult = DiagnosticAnalysisResult.Create(
                            project,
                            version,
                            syntaxLocalMap,
                            semanticLocalMap,
                            nonLocalMap: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                            others: ImmutableArray<DiagnosticData>.Empty,
                            documentIds);
                    }
                    else
                    {
                        if (!TryReadDiagnosticDataMap(reader, diagnosticDataSerializer, project, cancellationToken, out var syntaxLocalMap) ||
                            !TryReadDiagnosticDataMap(reader, diagnosticDataSerializer, project, cancellationToken, out var semanticLocalMap) ||
                            !TryReadDiagnosticDataMap(reader, diagnosticDataSerializer, project, cancellationToken, out var nonLocalMap) ||
                            !diagnosticDataSerializer.TryReadDiagnosticData(reader, project, document: null, cancellationToken, out var others))
                        {
                            return false;
                        }

                        analysisResult = DiagnosticAnalysisResult.Create(
                            project,
                            version,
                            syntaxLocalMap,
                            semanticLocalMap,
                            nonLocalMap,
                            others.NullToEmpty(),
                            documentIds: null);
                    }

                    analysisMap.Add(analyzer, analysisResult);
                }

                var telemetryMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();

                var telemetryCount = reader.ReadInt32();
                for (var i = 0; i < telemetryCount; i++)
                {
                    var analyzer = analyzerMap[reader.ReadString()];
                    var telemetryInfo = ReadTelemetry(reader, cancellationToken);

                    telemetryMap.Add(analyzer, telemetryInfo);
                }

                result = DiagnosticAnalysisResultMap.Create(analysisMap.ToImmutable(), telemetryMap.ToImmutable());
                return true;
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceled(ex))
            {
                return false;
            }
        }

        private static int WriteDiagnosticDataMap(
            ObjectWriter writer,
            DiagnosticDataSerializer serializer,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> diagnostics,
            CancellationToken cancellationToken)
        {
            var count = 0;

            writer.WriteInt32(diagnostics.Count);
            foreach (var (documentId, data) in diagnostics)
            {
                documentId.WriteTo(writer);
                serializer.WriteDiagnosticData(writer, data, cancellationToken);

                count += data.Length;
            }

            return count;
        }

        private static bool TryReadDiagnosticDataMap(
            ObjectReader reader,
            DiagnosticDataSerializer serializer,
            Project project,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? dataMap)
        {
            var count = reader.ReadInt32();

            var map = ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
            for (var i = 0; i < count; i++)
            {
                var documentId = DocumentId.ReadFrom(reader);
                var document = project.GetDocument(documentId);

                if (!serializer.TryReadDiagnosticData(reader, project, document, cancellationToken, out var diagnostics))
                {
                    dataMap = null;
                    return false;
                }

                // drop diagnostics for non-null document that doesn't support diagnostics
                if (diagnostics.IsDefault || document?.SupportsDiagnostics() == false)
                {
                    continue;
                }

                map.Add(documentId, diagnostics);
            }

            dataMap = map.ToImmutable();
            return true;
        }

        private static void WriteTelemetry(ObjectWriter writer, AnalyzerTelemetryInfo telemetryInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteInt32(telemetryInfo.CompilationStartActionsCount);
            writer.WriteInt32(telemetryInfo.CompilationEndActionsCount);
            writer.WriteInt32(telemetryInfo.CompilationActionsCount);
            writer.WriteInt32(telemetryInfo.SyntaxTreeActionsCount);
            writer.WriteInt32(telemetryInfo.AdditionalFileActionsCount);
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

        private static AnalyzerTelemetryInfo ReadTelemetry(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilationStartActionsCount = reader.ReadInt32();
            var compilationEndActionsCount = reader.ReadInt32();
            var compilationActionsCount = reader.ReadInt32();
            var syntaxTreeActionsCount = reader.ReadInt32();
            var additionalFileActionsCount = reader.ReadInt32();
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
                AdditionalFileActionsCount = additionalFileActionsCount,
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
    }
}
