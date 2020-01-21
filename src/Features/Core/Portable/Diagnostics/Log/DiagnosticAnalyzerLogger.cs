// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal sealed class DiagnosticAnalyzerLogger
    {
        private const string Id = nameof(Id);
        private const string AnalyzerCount = nameof(AnalyzerCount);
        private const string AnalyzerName = "Analyzer.Name";
        private const string AnalyzerHashCode = "Analyzer.NameHashCode";

        private static string ComputeSha256Hash(string name)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(name)));
        }

        public static void LogWorkspaceAnalyzers(ImmutableArray<AnalyzerReference> analyzers)
        {
            Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m =>
            {
                m[AnalyzerCount] = analyzers.Length;
            }));
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
                    var hasTelemetry = analyzerInfo.Telemetry;

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
    }
}
