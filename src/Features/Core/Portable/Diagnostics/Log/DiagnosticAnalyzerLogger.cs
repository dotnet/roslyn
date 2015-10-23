// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal class DiagnosticAnalyzerLogger
    {
        private const string Id = "Id";
        private const string AnalyzerCount = "AnalyzerCount";
        private const string AnalyzerName = "Analyzer.Name";
        private const string AnalyzerHashCode = "Analyzer.NameHashCode";
        private const string AnalyzerCrashCount = "Analyzer.CrashCount";
        private const string AnalyzerException = "Analyzer.Exception";
        private const string AnalyzerExceptionHashCode = "Analyzer.ExceptionHashCode";

        private static readonly SHA256CryptoServiceProvider s_sha256CryptoServiceProvider = GetSha256CryptoServiceProvider();
        private static readonly ConditionalWeakTable<DiagnosticAnalyzer, StrongBox<bool>> s_telemetryCache = new ConditionalWeakTable<DiagnosticAnalyzer, StrongBox<bool>>();

        private static string ComputeSha256Hash(string name)
        {
            if (s_sha256CryptoServiceProvider == null)
            {
                return "Hash Provider Not Available";
            }

            byte[] hash = s_sha256CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(name));
            return Convert.ToBase64String(hash);
        }

        public static void LogWorkspaceAnalyzers(ImmutableArray<AnalyzerReference> analyzers)
        {
            Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m =>
            {
                m[AnalyzerCount] = analyzers.Length;
            }));
        }

        public static void LogAnalyzerCrashCount(DiagnosticAnalyzer analyzer, Exception ex, LogAggregator logAggregator, ProjectId projectId)
        {
            if (logAggregator == null || analyzer == null || ex == null || ex is OperationCanceledException)
            {
                return;
            }

            // TODO: once we create description manager, pass that into here.
            bool telemetry = DiagnosticAnalyzerLogger.AllowsTelemetry(null, analyzer, projectId);
            var tuple = ValueTuple.Create(telemetry, analyzer.GetType(), ex.GetType());
            logAggregator.IncreaseCount(tuple);
        }

        public static void LogAnalyzerCrashCountSummary(int correlationId, LogAggregator logAggregator)
        {
            if (logAggregator == null)
            {
                return;
            }

            foreach (var analyzerCrash in logAggregator)
            {
                Logger.Log(FunctionId.DiagnosticAnalyzerDriver_AnalyzerCrash, KeyValueLogMessage.Create(m =>
                {
                    var key = (ValueTuple<bool, Type, Type>)analyzerCrash.Key;
                    bool telemetry = key.Item1;
                    m[Id] = correlationId;

                    // we log analyzer name and exception as it is, if telemetry is allowed
                    if (telemetry)
                    {
                        m[AnalyzerName] = key.Item2.FullName;
                        m[AnalyzerCrashCount] = analyzerCrash.Value.GetCount();
                        m[AnalyzerException] = key.Item3.FullName;
                    }
                    else
                    {
                        string analyzerName = key.Item2.FullName;
                        string exceptionName = key.Item3.FullName;

                        m[AnalyzerHashCode] = ComputeSha256Hash(analyzerName);
                        m[AnalyzerCrashCount] = analyzerCrash.Value.GetCount();
                        m[AnalyzerExceptionHashCode] = ComputeSha256Hash(exceptionName);
                    }
                }));
            }
        }

        public static void UpdateAnalyzerTypeCount(DiagnosticAnalyzer analyzer, AnalyzerTelemetryInfo analyzerTelemetryInfo, Project projectOpt, DiagnosticLogAggregator logAggregator)
        {
            if (analyzerTelemetryInfo == null || analyzer == null || logAggregator == null)
            {
                return;
            }

            logAggregator.UpdateAnalyzerTypeCount(analyzer, analyzerTelemetryInfo, projectOpt);
        }

        public static void LogAnalyzerTypeCountSummary(int correlationId, DiagnosticLogAggregator logAggregator)
        {
            if (logAggregator == null)
            {
                return;
            }

            foreach (var kvp in logAggregator.AnalyzerInfoMap)
            {
                Logger.Log(FunctionId.DiagnosticAnalyzerDriver_AnalyzerTypeCount, KeyValueLogMessage.Create(m =>
                {
                    m[Id] = correlationId;

                    var analyzerInfo = kvp.Value;
                    bool hasTelemetry = analyzerInfo.Telemetry;

                    // we log analyzer name as it is, if telemetry is allowed
                    if (hasTelemetry)
                    {
                        m[AnalyzerName] = analyzerInfo.CLRType.FullName;
                    }
                    else
                    {
                        // if it is from third party, we use hashcode
                        m[AnalyzerHashCode] = ComputeSha256Hash(analyzerInfo.CLRType.FullName);
                    }

                    for (var i = 0; i < analyzerInfo.Counts.Length; i++)
                    {
                        m[DiagnosticLogAggregator.AnalyzerTypes[i]] = analyzerInfo.Counts[i];
                    }
                }));
            }
        }

        public static bool AllowsTelemetry(DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer, ProjectId projectIdOpt)
        {
            StrongBox<bool> value;
            if (s_telemetryCache.TryGetValue(analyzer, out value))
            {
                return value.Value;
            }

            return s_telemetryCache.GetValue(analyzer, a => new StrongBox<bool>(CheckTelemetry(service, a))).Value;
        }

        private static bool CheckTelemetry(DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer)
        {
            if (analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            ImmutableArray<DiagnosticDescriptor> diagDescriptors;
            try
            {
                // SupportedDiagnostics is potentially user code and can throw an exception.
                diagDescriptors = service != null ? service.GetDiagnosticDescriptors(analyzer) : analyzer.SupportedDiagnostics;
            }
            catch (Exception)
            {
                return false;
            }

            if (diagDescriptors == null)
            {
                return false;
            }

            // find if the first diagnostic in this analyzer allows telemetry
            DiagnosticDescriptor diagnostic = diagDescriptors.Length > 0 ? diagDescriptors[0] : null;
            return diagnostic == null ? false : diagnostic.CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry);
        }

        private static SHA256CryptoServiceProvider GetSha256CryptoServiceProvider()
        {
            try
            {
                // not all environment allows SHA256 encryption
                return new SHA256CryptoServiceProvider();
            }
            catch
            {
                return null;
            }
        }
    }
}
