// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ErrorReporting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;
internal static class DiagnosticDataExtensions
{
    internal static bool TryGetUnnecessaryDataLocations(this DiagnosticData diagnosticData, [NotNullWhen(true)] out ImmutableArray<DiagnosticDataLocation>? unnecessaryLocations)
    {
        // If there are 'unnecessary' locations specified in the property bag, use those instead of the main diagnostic location.
        if (diagnosticData.TryGetUnnecessaryLocationIndices(out var unnecessaryIndices))
        {
            using var _ = PooledObjects.ArrayBuilder<DiagnosticDataLocation>.GetInstance(out var locationsToTag);

            foreach (var index in GetLocationIndices(unnecessaryIndices))
                locationsToTag.Add(diagnosticData.AdditionalLocations[index]);

            unnecessaryLocations = locationsToTag.ToImmutable();
            return true;
        }

        unnecessaryLocations = null;
        return false;

        static IEnumerable<int> GetLocationIndices(string indicesProperty)
        {
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(indicesProperty));
                var serializer = new DataContractJsonSerializer(typeof(IEnumerable<int>));
                var result = serializer.ReadObject(stream) as IEnumerable<int>;
                return result ?? Array.Empty<int>();
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                return ImmutableArray<int>.Empty;
            }
        }
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
