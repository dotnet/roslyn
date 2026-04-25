// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;

internal static class DiagnosticDataExtensions
{
    internal static bool TryGetUnnecessaryDataLocations(this DiagnosticData diagnosticData, [NotNullWhen(true)] out ImmutableArray<DiagnosticDataLocation>? unnecessaryLocations)
    {
        // If there are 'unnecessary' locations specified in the property bag, use those instead of the main diagnostic location.
        if (diagnosticData.TryGetUnnecessaryLocationIndices(out var unnecessaryIndices))
        {
            using var _ = PooledObjects.ArrayBuilder<DiagnosticDataLocation>.GetInstance(out var locationsToTag);

            try
            {
                // Parse a JSON array of non-negative integers (e.g., "[1,2,3]") inline. This replaces
                // DataContractJsonSerializer which was extremely allocation-heavy for this simple format.
                if (unnecessaryIndices.Length >= 2 && unnecessaryIndices[0] == '[' && unnecessaryIndices[^1] == ']')
                {
                    var start = 1;
                    var end = unnecessaryIndices.Length - 1;
                    while (start < end)
                    {
                        var commaIndex = unnecessaryIndices.IndexOf(',', start, end - start);
                        var elementEnd = commaIndex < 0 ? end : commaIndex;

                        var index = int.Parse(unnecessaryIndices.AsSpan(start, elementEnd - start).Trim().ToString());

                        locationsToTag.Add(diagnosticData.AdditionalLocations[index]);
                        start = elementEnd + 1;
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                locationsToTag.Clear();
            }

            unnecessaryLocations = locationsToTag.ToImmutable();
            return true;
        }

        unnecessaryLocations = null;
        return false;
    }

    internal static bool TryGetUnnecessaryLocationIndices(
            this DiagnosticData diagnosticData, [NotNullWhen(true)] out string? unnecessaryIndices)
    {
        unnecessaryIndices = null;

        return diagnosticData.AdditionalLocations.Length > 0
            && diagnosticData.Properties != null
            && diagnosticData.Properties.TryGetValue(WellKnownDiagnosticTags.Unnecessary, out unnecessaryIndices)
            && unnecessaryIndices != null;
    }
}
