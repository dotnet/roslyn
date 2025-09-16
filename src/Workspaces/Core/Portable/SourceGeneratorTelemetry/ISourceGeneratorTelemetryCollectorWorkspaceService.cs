// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SourceGeneratorTelemetry;

internal interface ISourceGeneratorTelemetryCollectorWorkspaceService : IWorkspaceService
{
    void CollectRunResult(GeneratorDriverRunResult driverRunResult, GeneratorDriverTimingInfo driverTimingInfo, Func<ISourceGenerator, AnalyzerReference> getAnalyzerReference);

    /// <summary>
    /// Returns a list of telemetry keys, one set of keys for each generator that was ran.
    /// </summary>
    /// <remarks>
    /// The objects in the keys are either strings or numbers like integers or floats, so they should be serializable in any reasonable format.
    /// </remarks>
    ImmutableArray<ImmutableDictionary<string, object?>> FetchKeysAndAndClear();
}
