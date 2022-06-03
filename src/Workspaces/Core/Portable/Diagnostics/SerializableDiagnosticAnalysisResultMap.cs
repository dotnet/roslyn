﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [DataContract]
    internal readonly struct SerializableDiagnosticAnalysisResults
    {
        public static readonly SerializableDiagnosticAnalysisResults Empty = new(
            ImmutableArray<(string, SerializableDiagnosticMap)>.Empty,
            ImmutableArray<(string, AnalyzerTelemetryInfo)>.Empty);

        [DataMember(Order = 0)]
        internal readonly ImmutableArray<(string analyzerId, SerializableDiagnosticMap diagnosticMap)> Diagnostics;

        [DataMember(Order = 1)]
        internal readonly ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo telemetry)> Telemetry;

        public SerializableDiagnosticAnalysisResults(
            ImmutableArray<(string analyzerId, SerializableDiagnosticMap diagnosticMap)> diagnostics,
            ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)> telemetry)
        {
            Diagnostics = diagnostics;
            Telemetry = telemetry;
        }
    }

    [DataContract]
    internal readonly struct SerializableDiagnosticMap
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> Syntax;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> Semantic;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> NonLocal;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<DiagnosticData> Other;

        public SerializableDiagnosticMap(
            ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> syntax,
            ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> semantic,
            ImmutableArray<(DocumentId, ImmutableArray<DiagnosticData>)> nonLocal,
            ImmutableArray<DiagnosticData> other)
        {
            Syntax = syntax;
            Semantic = semantic;
            NonLocal = nonLocal;
            Other = other;
        }
    }
}
