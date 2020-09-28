// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    internal static class DiagnosticUtilities
    {
        public static VSDiagnostic Convert(SourceText text, DiagnosticData diagnosticData)
        {
            Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");
            Contract.ThrowIfNull(diagnosticData.DataLocation, $"Got a document diagnostic that did not have a {nameof(diagnosticData.DataLocation)}");

            return new VSDiagnostic
            {
                Code = diagnosticData.Id,
                Message = diagnosticData.Message,
                Severity = ProtocolConversions.DiagnosticSeverityToLspDiagnositcSeverity(diagnosticData.Severity),
                Range = ProtocolConversions.LinePositionToRange(DiagnosticData.GetLinePositionSpan(diagnosticData.DataLocation, text, useMapped: true)),
                Tags = DiagnosticUtilities.ConvertTags(diagnosticData),
                DiagnosticType = diagnosticData.Category,
            };
        }

        private static DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

            if (diagnosticData.Severity == DiagnosticSeverity.Hidden)
            {
                result.Add(VSDiagnosticTags.HiddenInEditor);
                result.Add(VSDiagnosticTags.HiddenInErrorList);
            }

            foreach (var tag in diagnosticData.CustomTags)
            {
                switch (tag)
                {
                    case WellKnownDiagnosticTags.Unnecessary:
                        result.Add(DiagnosticTag.Unnecessary);
                        break;
                    case WellKnownDiagnosticTags.Build:
                        result.Add(VSDiagnosticTags.BuildError);
                        break;
                }
            }

            return result.ToArray();
        }
    }
}
